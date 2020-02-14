using System;
using System.Collections.Generic;
using System.Reflection;
using TwinCAT.TypeSystem;

/// <summary>
/// TwinCAT ADS Extension Methods
/// Author: Brandon Stiffler
/// 
/// Development versions
/// .NET Framework:         4.5.2
/// TwinCAT.Ads Assembly:   4.2.169.0
/// </summary>
namespace TcAdsExtensions.ADS
{
    /// <summary>
    /// ISymbol extension methods
    /// </summary>
    public static class SymbolExtensions
    {
        /// <summary>
        /// Writes to ADS symbol from custom object type.
        /// </summary>
        /// <param name="Symbol">ADS Symbol to write</param>
        /// <param name="Data">User defined object (class or struct with matching properties) to write to Symbol</param>
        public static void SerializeObject(this ISymbol Symbol, object Object)
        {
            // cast to IValueSymbol type (for read/write)
            IValueSymbol symbol = (IValueSymbol)Symbol;

            // if primitive type
            if (symbol.IsPrimitiveType)
            {
                // write object to symbol value
                symbol.WriteValue(Object);
            }
            else
            {
                // symbol is array of non-primitive (struct array)
                if (symbol.Category == DataTypeCategory.Array)
                {
                    // validate Object parameter is array
                    if (Object.GetType().IsArray)
                    {
                        // cast Object to array var for indexing
                        var arr = (Array)Object;

                        // loop through elements (with upper bounds check)
                        for (int i = 0; i < Math.Max(symbol.SubSymbols.Count, arr.Length); i++)
                        {
                            // get Object element, call recursively
                            if (arr.GetValue(i) != null)
                                SerializeObject(symbol.SubSymbols[i], arr.GetValue(i));
                        }
                    }
                    else
                    {
                        // array type mismatch exception
                        throw new Exception("Type mismatch: Object parameter is not array type.");
                    }
                }
                else
                {
                    foreach (ISymbol symb in symbol.SubSymbols)
                    {
                        // get IValueSymbol
                        IValueSymbol vSymb = (IValueSymbol)symb;

                        // check if Object parameter contains matching property
                        var property = Object.GetType().GetProperty(vSymb.InstanceName);
                        if (property != null)
                        {
                            // if symbol is primitive
                            if (vSymb.IsPrimitiveType)
                            {
                                // write object property value to symbol
                                vSymb.WriteValue(property.GetValue(Object));
                            }
                            else
                            {
                                // if sub symbol is array type
                                if (vSymb.Category == DataTypeCategory.Array)
                                {
                                    // cast property to array var for indexing
                                    var arr = (Array)property.GetValue(Object);

                                    // loop through elements (with upper bounds check)
                                    for (int i = 0; i < Math.Max(vSymb.SubSymbols.Count, arr.Length); i++)
                                    {
                                        // if property array index is initialized, call recursively
                                        if (arr.GetValue(i) != null)
                                            SerializeObject(vSymb.SubSymbols[i], arr.GetValue(i));

                                    }
                                }
                                else
                                {
                                    // call recursively on struct
                                    SerializeObject(vSymb, property.GetValue(Object));
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Reads ADS Symbol value(s) into instance of type T.
        /// </summary>
        /// <typeparam name="T">Type (class or structure definition)</typeparam>
        /// <param name="Symbol">ADS structure Symbol to read</param>
        /// <returns>Object of type T with values read from Symbol</returns>
        public static T DeserializeObject<T>(this ISymbol Symbol) where T : new()
        {
            // cast to IValueSymbol type
            IValueSymbol symbol = (IValueSymbol)Symbol;

            // instantiate new object
            var obj = new T();

            // if primitive / non-struct type
            if (symbol.IsPrimitiveType)
            {
                // cast to appropriate type and return
                return (T)symbol.ReadValue();
            }
            else
            {
                // loop through subsymbols in struct
                foreach(IValueSymbol vSymb in symbol.SubSymbols)
                {
                    // check if T object has matching property
                    var property = typeof(T).GetProperty(vSymb.InstanceName);
                    if (property != null)
                    {
                        // if symbol is primitive
                        if (vSymb.IsPrimitiveType)
                        {
                            // set object property value from sub symbol
                            property.SetValue(obj, vSymb.ReadValue());
                        }
                        else
                        {
                            // if sub symbol is array type
                            if (vSymb.Category == DataTypeCategory.Array)
                            {
                                // get object array property type
                                Type propertyType = property.PropertyType;
                                // get array element type
                                Type elementType = property.PropertyType.GetElementType();

                                // instantiate new array of property type
                                var propVal = Activator.CreateInstance(propertyType, new object[] { vSymb.SubSymbols.Count });

                                // initialize generic method for recursive call (reflection)
                                MethodInfo method = typeof(SymbolExtensions).GetMethod("DeserializeArrayObject",
                                    BindingFlags.Public | BindingFlags.Static);
                                MethodInfo generic = method.MakeGenericMethod(elementType);

                                // call array method recursively
                                propVal = generic.Invoke(null, new object[] { vSymb });

                                // set property value
                                property.SetValue(obj, propVal);
                            }
                            else
                            {
                                // get object property type
                                Type propertyType = property.PropertyType;

                                // create instance of property
                                var propVal = Activator.CreateInstance(propertyType);

                                // initialize generic method for recursive call (reflection)
                                MethodInfo method = typeof(SymbolExtensions).GetMethod("DeserializeObject",
                                    BindingFlags.Public | BindingFlags.Static);
                                MethodInfo generic = method.MakeGenericMethod(propertyType);

                                // call method recursively
                                propVal = generic.Invoke(null, new object[] { vSymb });

                                // set property value
                                property.SetValue(obj, propVal);
                            }
                        }
                    }
                }
            }

            // return generated instance of type T
            return obj;
        }


        /// <summary>
        /// Reads ADS Array Symbol value(s) into instance of type T[].
        /// </summary>
        /// <typeparam name="T">Type (class or struct Array definition)</typeparam>
        /// <param name="Symbol">ADS array symbol to read</param>
        /// <returns>Object array of type T with values read from Symbol</returns>
        public static T[] DeserializeArrayObject<T>(this ISymbol Symbol) where T : new()
        {
            // cast to IValueSymbol type
            IValueSymbol symbol = (IValueSymbol)Symbol;

            // instantiate new object array of symbol count size
            var obj = new T[symbol.SubSymbols.Count];

            // if primitive / non-struct type
            if (symbol.IsPrimitiveType)
            {
                // cast to appropriate type and return
                return (T[])symbol.ReadValue();
            }
            else
            {
                // iterate through each symbol array element
                for (int i = 0; i < symbol.SubSymbols.Count; i++)
                {
                    // call DeserializeObject on each element
                    obj[i] = DeserializeObject<T>(symbol.SubSymbols[i]);
                }
            }

            // return generated array of type T
            return obj;
        }


        /// <summary>
        /// Reads ADS Symbol into Dictionary of key/value pairs.
        /// Experimental. Very slow.
        /// </summary>
        /// <param name="Symbol">ADS symbol to read</param>
        /// <returns>Dictionary of key/value pairs</returns>
        public static Dictionary<string, object> ToDictionary(this ISymbol Symbol)
        {
            // cast to IValueSymbol
            IValueSymbol symbol = (IValueSymbol)Symbol;

            // create new dictionary
            Dictionary<string, object> dict = new Dictionary<string, object>();

            // primitive types
            if (symbol.IsPrimitiveType)
            {
                dict.Add(symbol.InstanceName, symbol.ReadValue());
            }
            else
            {
                // array of structs
                if (symbol.Category == DataTypeCategory.Array)
                {
                    // declare sub element array for parallelization
                    // calling dict.Add() in parallel loop will result in
                    // out-of-order elements
                    var sub = new KeyValuePair<string, object>[symbol.SubSymbols.Count];

                    // loop through struct array and call recursively
                    for (int i = 0; i < symbol.SubSymbols.Count; i++)
                    {
                        sub[i] = new KeyValuePair<string, object>(symbol.SubSymbols[i].InstanceName, symbol.SubSymbols[i].ToDictionary());
                    }

                    // add loaded KVPs into dictionary
                    foreach (KeyValuePair<string, object> pair in sub)
                    {
                        dict.Add(pair.Key, pair.Value);
                    }
                }
                // struct
                else if (symbol.Category == DataTypeCategory.Struct)
                {
                    foreach (IValueSymbol symb in symbol.SubSymbols)
                    {
                        if (symb.IsPrimitiveType)
                            dict.Add(symb.InstanceName, symb.ReadValue());
                        else
                            dict.Add(symb.InstanceName, symb.ToDictionary());
                    }
                }
            }


            return dict;
        }
    }
}

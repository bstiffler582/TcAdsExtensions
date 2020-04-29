using System;
using System.ComponentModel;
using System.Collections.Generic;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace TcAdsExtensions.ADS
{
    class AdsConnection : IDisposable
    {
        private TcAdsClient _client;
        private ISymbolLoader _symbolLoader;

        // list of global event symbols
        private List<string> _eventSymbols = new List<string>();

        // used for custom individual value change events
        private EventHandlerList _dynamicEvents = new EventHandlerList();

        // used for single global value change event
        public event EventHandler<object> OnSymbolValueChanged;

        public bool IsConnected
        {
            get
            {
                if (_client != null)
                    return _client.IsConnected;
                else return false;
            }
        }

        /// <summary>
        /// Constructor - establishes connection and creates symbol loader
        /// </summary>
        /// <param name="AmsNetId">ADS Client AMS Net Id "x.x.x.x.x.x:port" (127.0.0.1.1.1:851)</param>
        public AdsConnection(string AmsNetId)
        {
            _client = new TcAdsClient();

            // connect ADS client
            _client.Connect(AmsAddress.Parse(AmsNetId));

            if (_client.IsConnected)
            {
                // create symbol loader
                _symbolLoader = SymbolLoaderFactory.Create(_client,
                    new SymbolLoaderSettings(SymbolsLoadMode.VirtualTree));
            }
        }

        /// <summary>
        /// Raises 'Method' action when SymbolPath value changes
        /// </summary>
        /// <param name="SymbolName">Path of Symbol to poll for value change</param>
        /// <param name="Method">Action / method executed on value change</param>
        public void SubscribeOnValueChange(string SymbolPath, Action<object> Method)
        {

            if (_dynamicEvents[SymbolPath] == null)
            {
                IValueSymbol symbol = (IValueSymbol)_symbolLoader.Symbols[SymbolPath];
                symbol.ValueChanged += Symbol_ValueChanged;
                _dynamicEvents.AddHandler(symbol.InstancePath, Method);
            }
        }

        /// <summary>
        /// Raises OnSymbolValueChanged event when SymbolPath value changes
        /// </summary>
        /// <param name="SymbolName">Path of Symbol to poll for value change</param>
        public void SubscribeOnValueChange(string SymbolPath)
        {
            if (_eventSymbols == null) _eventSymbols = new List<string>();

            if (!_eventSymbols.Contains(SymbolPath))
            {
                IValueSymbol symbol = (IValueSymbol)_symbolLoader.Symbols[SymbolPath];

                symbol.ValueChanged += Symbol_ValueChanged;
                _eventSymbols.Add(symbol.InstancePath);
            }
        }

        /// <summary>
        /// Symbol Value Change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Symbol_ValueChanged(object sender, ValueChangedArgs e)
        {
            ISymbol symbol = (ISymbol)sender;

            // invoke OnValueChanged event if symbol is in list
            if (_eventSymbols.Contains(symbol.InstancePath))
                OnSymbolValueChanged?.Invoke(symbol.InstancePath, e.Value);

            // invoke custom event if registered
            if (_dynamicEvents[symbol.InstancePath] != null)
                _dynamicEvents[symbol.InstancePath]?.DynamicInvoke(e.Value);
        }

        /// <summary>
        /// Reads value of primitive-type symbol
        /// </summary>
        /// <typeparam name="T">Symbol primitive type</typeparam>
        /// <param name="SymbolPath">Symbol path</param>
        /// <returns>Value of symbol</returns>
        public T ReadPrimitiveSymbol<T>(string SymbolPath)
        {
            IValueSymbol symbol = (IValueSymbol)_symbolLoader.Symbols[SymbolPath];
            return (T)symbol.ReadValue();
        }

        /// <summary>
        /// Reads value of primitive-type symbol
        /// </summary>
        /// <param name="SymbolPath">Symbol path</param>
        /// <returns>Value of symbol</returns>
        public object ReadPrimitiveSymbol(string SymbolPath)
        {
            IValueSymbol symbol = (IValueSymbol)_symbolLoader.Symbols[SymbolPath];
            return symbol.ReadValue();
        }

        /// <summary>
        /// Writes Value to primitive-type symbol
        /// </summary>
        /// <param name="SymbolPath">Symbol path</param>
        /// <param name="Value">Value to write</param>
        public void WritePrimitiveSymbol(string SymbolPath, object Value)
        {
            IValueSymbol symbol = (IValueSymbol)_symbolLoader.Symbols[SymbolPath];
            symbol.WriteValue(Value);
        }

        /// <summary>
        /// Reads non-primitive / structure type symbol
        /// </summary>
        /// <typeparam name="T">Class definition to match structure</typeparam>
        /// <param name="SymbolPath">Symbol path</param>
        /// <returns>Instance of T with matching property values</returns>
        public T ReadStructSymbol<T>(string SymbolPath) where T : new()
        {
            try
            {
                return _symbolLoader.Symbols[SymbolPath].DeserializeObject<T>();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>
        /// Writes to non-primitive / structure type symbol
        /// </summary>
        /// <param name="SymbolPath">Symbol path</param>
        /// <param name="Object">Object value(s) to write</param>
        public void WriteStructSymbol(string SymbolPath, object Object)
        {
            _symbolLoader.Symbols[SymbolPath].SerializeObject(Object);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                if (_eventSymbols.Count > 0)
                {
                    // unregister events
                    foreach (string symbol in _eventSymbols)
                    {
                        IValueSymbol symb = (IValueSymbol)_symbolLoader.Symbols[symbol];
                        symb.ValueChanged -= Symbol_ValueChanged;
                    }
                }

                // dispose custom event container
                _dynamicEvents.Dispose();


                // disconnect client
                if (_client.IsConnected)
                    _client.Disconnect();

                _client.Dispose();
            }
        }
    }
}

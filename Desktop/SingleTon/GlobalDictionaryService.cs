using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.SingleTon
{
    public class GlobalDictionaryService
    {
        private Dictionary<string, bool> _dictionaryService = new Dictionary<string, bool>();

        private static GlobalDictionaryService _instance;
        private static readonly object _lock = new object();

        private GlobalDictionaryService() { }

        public static GlobalDictionaryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlobalDictionaryService();
                        }
                    }
                }
                return _instance;
            }
        }

        public Dictionary<string, bool> DictionaryService
        {
            get { return _dictionaryService; }
        }

    }
}

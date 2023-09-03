using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.SingleTon
{
    public class GlobalVariables
    {
        private static GlobalVariables _instance;
        private static readonly object _lock = new object();

        private bool _isLogicExecuted_EdgeCache = false;
        private bool _isLogicExecuted_ChromeCache = false;
        private bool _isLogicExecuted_OperaCache = false;
        private bool _isLogicExecuted_FirefoxCache = false;

        private bool _isLogicExecuted_EdgeCookies = false;
        private bool _isLogicExecuted_ChromeCookies = false;
        private bool _isLogicExecuted_OperaCookies = false;
        private bool _isLogicExecuted_FirefoxCookies = false;


        private bool _isLogicExecuted_EdgeHistory = false;
        private bool _isLogicExecuted_ChromeHistory = false;
        private bool _isLogicExecuted_OperaHistory = false;
        private bool _isLogicExecuted_FirefoxHistory = false;

        private GlobalVariables() { }

        public static GlobalVariables Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlobalVariables();
                        }
                    }
                }
                return _instance;
            }
        }

        //-------Cache variables-----------//
        public bool IsLogicExecuted_ChromeCache
        {
            get { return _isLogicExecuted_ChromeCache; }
            set { _isLogicExecuted_ChromeCache = value; }
        }

        public bool IsLogicExecuted_EdgeCache
        {
            get { return _isLogicExecuted_EdgeCache; }
            set { _isLogicExecuted_EdgeCache = value; }
        }

        public bool IsLogicExecuted_FirefoxCache
        {
            get { return _isLogicExecuted_FirefoxCache; }
            set { _isLogicExecuted_FirefoxCache = value; }
        }

        public bool IsLogicExecuted_OperaCache
        {
            get { return _isLogicExecuted_OperaCache; }
            set { _isLogicExecuted_OperaCache = value; }
        }


        //-------Cookies variables-----------//
        public bool IsLogicExecuted_ChromeCookies
        {
            get { return _isLogicExecuted_ChromeCookies; }
            set { _isLogicExecuted_ChromeCookies = value; }
        }

        public bool IsLogicExecuted_EdgeCookies
        {
            get { return _isLogicExecuted_EdgeCookies; }
            set { _isLogicExecuted_EdgeCookies = value; }
        }

        public bool IsLogicExecuted_FirefoxCookies
        {
            get { return _isLogicExecuted_FirefoxCookies; }
            set { _isLogicExecuted_FirefoxCookies = value; }
        }

        public bool IsLogicExecuted_OperaCookies
        {
            get { return _isLogicExecuted_OperaCookies; }
            set { _isLogicExecuted_OperaCookies = value; }
        }

        //-------History variables-----------//
        public bool IsLogicExecuted_ChromeHistory
        {
            get { return _isLogicExecuted_ChromeHistory; }
            set { _isLogicExecuted_ChromeHistory = value; }
        }

        public bool IsLogicExecuted_EdgeHistory
        {
            get { return _isLogicExecuted_EdgeHistory; }
            set { _isLogicExecuted_EdgeHistory = value; }
        }

        public bool IsLogicExecuted_FirefoxHistory
        {
            get { return _isLogicExecuted_FirefoxHistory; }
            set { _isLogicExecuted_FirefoxHistory = value; }
        }

        public bool IsLogicExecuted_OperaHistory
        {
            get { return _isLogicExecuted_OperaHistory; }
            set { _isLogicExecuted_OperaHistory = value; }
        }

        public bool HasTrueProperty()
        {

            bool result = (_isLogicExecuted_OperaHistory && _isLogicExecuted_OperaCookies) || (_isLogicExecuted_OperaHistory && _isLogicExecuted_OperaCache) || (_isLogicExecuted_OperaCookies && _isLogicExecuted_OperaCache);

            bool result2 = (_isLogicExecuted_ChromeHistory && _isLogicExecuted_ChromeCookies) || (_isLogicExecuted_ChromeHistory && _isLogicExecuted_ChromeCache) || (_isLogicExecuted_ChromeCookies && _isLogicExecuted_ChromeCache);

            bool result3 = (_isLogicExecuted_EdgeHistory && _isLogicExecuted_EdgeCookies) || (_isLogicExecuted_EdgeHistory && _isLogicExecuted_EdgeCache) || (_isLogicExecuted_EdgeCookies && _isLogicExecuted_EdgeCache);

            bool result4 = (_isLogicExecuted_FirefoxHistory && _isLogicExecuted_FirefoxCookies) || (_isLogicExecuted_FirefoxHistory && _isLogicExecuted_FirefoxCache) || (_isLogicExecuted_FirefoxCookies && _isLogicExecuted_FirefoxCache);

            return result || result2 || result3 || result4;
        }

    }
}

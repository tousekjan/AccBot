using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoBotCore.Models
{
        public class MarketPriceSnapshot
        {
            public DateTime MARPS_DateTime { get; set; }
            public Guid MARPS_ApplicationId { get; set; }
            public string MARPS_PLATF_Platform_Key { get; set; }
            public string MARPS_CURRE_CurrencyFrom_Key { get; set; }
            public string MARPS_CURRE_CurrencyTo_Key { get; set; }
            public decimal? MARPS_CurrencyFrom_Buy { get; set; }
            public decimal? MARPS_CurrencyFrom_Sell { get; set; }
            public decimal? MARPS_CurrencyCZK_Buy { get; set; }
            public decimal? MARPS_CurrencyCZK_Sell { get; set; }
            public decimal? MARPS_CurrencyCZK_ExchangeRate { get; set; }
            public int MARPS_Id { get; set; }
            public virtual Currency Currency { get; set; }
            public virtual Currency Currency1 { get; set; }
            public virtual Plaftorm Plaftorm { get; set; }
        }
}
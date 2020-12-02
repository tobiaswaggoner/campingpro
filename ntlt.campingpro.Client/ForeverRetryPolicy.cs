// *********************************************************
// (c) 2020 - 2020 Netzalist GmbH & Co.KG
// *********************************************************

using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace ntlt.campingpro.Client
{
    public class ForeverRetryPolicy : IRetryPolicy
    {
        private readonly Random _random = new Random();

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var retryDelay = retryContext.ElapsedTime < TimeSpan.FromSeconds(60) ? _random.Next(2,5) : _random.Next(5,20);
            Console.WriteLine($"Delaying next reconnect by {retryDelay} seconds");
            return TimeSpan.FromSeconds(retryDelay);
        }
    }
}
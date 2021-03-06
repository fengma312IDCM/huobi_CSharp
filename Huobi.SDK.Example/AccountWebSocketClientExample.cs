﻿using System;
using Huobi.SDK.Core.Client;
using Huobi.SDK.Model.Response;
using Huobi.SDK.Model.Response.Account;
using Huobi.SDK.Model.Response.Auth;
using Huobi.SDK.Model.Response.WebSocket;

namespace Huobi.SDK.Example
{
    public class AccountWebSocketClientExample
    {
        public static void RunAll()
        {
            RequestAccount();

            SubscribeAccountV1();

            SubscribeAccountV2();
        }

        private static void RequestAccount()
        {
            // Initialize a new instance
            var client = new RequestAccountWebSocketV1ClientV(Config.AccessKey, Config.SecretKey);

            // Add the auth receive handler
            client.OnAuthenticationReceived += Client_OnAuthReceived;
            void Client_OnAuthReceived(WebSocketV1AuthResponse response)
            {
                if (response.errCode == 0)
                {
                    // Request full data if authentication passed
                    client.Request();
                    Console.WriteLine("Request sent");
                }
            }

            // Add the data receive handler
            client.OnDataReceived += Client_OnDataReceived;
            void Client_OnDataReceived(RequestAccountResponse response)
            {
                if (response != null && response.data != null)
                {
                    foreach (var a in response.data)
                    {
                        Console.WriteLine($"account id: {a.id}, type: {a.type}, state: {a.state}");
                        if (a.list != null)
                        {
                            foreach (var b in a.list)
                            {
                                Console.WriteLine($"currency: {b.currency}, type: {b.type}, balance: {b.balance}");
                            }
                        }
                    }
                }
            }

            // Then connect to server and wait for the handler to handle the response
            client.Connect(false);

            Console.WriteLine("Press ENTER to quit...\n");
            Console.ReadLine();

            // Delete handler
            client.OnDataReceived -= Client_OnDataReceived;
        }

        private static void SubscribeAccountV1()
        {
            // Initialize a new instance
            var client = new SubscribeAccountWebSocketV1Client(Config.AccessKey, Config.SecretKey);

            // Add the auth receive handler
            client.OnAuthenticationReceived += Client_OnAuthReceived;
            void Client_OnAuthReceived(WebSocketV1AuthResponse response)
            {
                if (response.errCode == 0)
                {
                    // Subscribe the specific topic
                    client.Subscribe("1");
                    Console.WriteLine("Subscription sent");
                }
            }

            // Add the data receive handler
            client.OnDataReceived += Client_OnDataReceived;
            void Client_OnDataReceived(SubscribeAccountV1Response response)
            {
                if (response != null && response.data != null)
                {
                    Console.WriteLine($"Account update: {response.data.@event}");
                    if (response.data.list != null)
                    {
                        foreach (var b in response.data.list)
                        {
                            Console.WriteLine($"account id: {b.accountId}, currency: {b.currency}, type: {b.type}, balance: {b.balance}");
                        }
                    }
                }
            }

            // Then connect to server and wait for the handler to handle the response
            client.Connect();

            Console.WriteLine("Press ENTER to unsubscribe and stop...\n");
            Console.ReadLine();

            // Unsubscrive the specific topic
            client.UnSubscribe("1");

            // Delete handler
            client.OnDataReceived -= Client_OnDataReceived;
        }

        private static void SubscribeAccountV2()
        {
            // Initialize a new instance
            var client = new SubscribeAccountWebSocketV2Client(Config.AccessKey, Config.SecretKey);

            // Add the auth receive handler
            client.OnAuthenticationReceived += Client_OnAuthReceived;
            void Client_OnAuthReceived(WebSocketV2AuthResponse response)
            {
                if (response.code == (int)ResponseCode.Success)
                {
                    // Subscribe the specific topic
                    client.Subscribe("1");
                    Console.WriteLine("Subscription sent");
                }
            }

            // Add the data receive handler
            client.OnDataReceived += Client_OnDataReceived;
            void Client_OnDataReceived(SubscribeAccountV2Response response)
            {
                if (response != null)
                {
                    if (response.action.Equals("sub"))
                    {
                        if (response.code == (int)ResponseCode.Success)
                        {
                            Console.WriteLine($"Subscribe topic {response.ch} successfully");
                        }
                        else
                        {
                            Console.WriteLine($"Subscribe topic {response.ch} fail, error code: {response.code}, error message: {response.message}");
                        }
                    }
                    else if (response.action.Equals("push") && response.data != null)
                    {
                        var b = response.data;
                        if (b.changeTime == null)
                        {
                            Console.WriteLine($"Account overview, currency: {b.currency}, id: {b.accountId}, balance: {b.balance}");
                        }
                        else
                        {
                            Console.WriteLine($"Account update, currency: {b.currency}, id: {b.accountId}, balance: {b.balance}, time: {b.changeTime}");
                        }
                    }
                }
            }

            // Then connect to server and wait for the handler to handle the response
            client.Connect();

            Console.WriteLine("Press ENTER to unsubscribe and stop...\n");
            Console.ReadLine();

            // Unsubscrive the specific topic
            client.UnSubscribe("1");

            // Delete handler
            client.OnDataReceived -= Client_OnDataReceived;
        }
    }
}

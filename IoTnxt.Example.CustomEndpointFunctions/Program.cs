using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using IoTnxt.DAPI.RedGreenQueue.Adapter;
using IoTnxt.DAPI.RedGreenQueue.Extensions;
using IoTnxt.DAPI.RedGreenQueue.Proxy;
using IoTnxt.Gateway.API.Abstractions;
using IoTnxt.RedGreenQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTnxt.Example.CustomEndpointFunctions
{
    public class Program
    {
        //GatewayId needs to unique identify your service (and its resulting gateway)
        //A gateway only connects to one IoT.nxt tenant - provision a separate gateway for each different IoT.nxt tenant
        public static string GatewayId = (
            from nic in NetworkInterface.GetAllNetworkInterfaces()
            where nic.OperationalStatus == OperationalStatus.Up
            select nic.GetPhysicalAddress().ToString()
        ).FirstOrDefault() + "FUNCTIONS";

        //Secret key is your password.  It should be strong and secure and never shared except during registration.
        //It cannot be changed once you have registered the gateway.
        //Replace this with a strong password!
        public static string SecretKey = Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(GatewayId)));

        private static void Main()
        {
            Console.WriteLine($"Gateway Id: {GatewayId}");

            var services = new ServiceCollection();

            services.AddOptions();
            services.AddLogging(lb => lb.AddConsole());
            services.AddDapiRedGreenQueue(new ConfigurationBuilder().Build());
            services.AddDapiRedGreenQueueProxy().AddSingletonProxy<IGatewayApi>();
            services.AddSingleton<ExampleCustomFunction>();

            services.Configure<RedGreenQueueAdapterOptions>(cfg =>
            {
                cfg.GreenQueueOptions = new GreenQueueOptions
                {
                    Hosts = new List<string> { "greenqueue.prod.iotnxt.io" },
                    ServiceUniqueId = GatewayId,
                    SecretKey = SecretKey,
                    publicKeyAsXml =
                        "<RSAKeyValue><Exponent>AQAB</Exponent><Modulus>rbltknM3wO5/TAEigft0RDlI6R9yPttweDXtmXjmpxwcuVtqJgNbIQ3VduGVlG6sOg20iEbBWMCdwJ3HZTrtn7qpXRdJBqDUNye4Xbwp1Dp+zMFpgEsCklM7c6/iIq14nymhNo9Cn3eBBM3yZzUKJuPn9CTZSOtCenSbae9X9bnHSW2qB1qRSQ2M03VppBYAyMjZvP1wSDVNuvCtjU2Lg/8o/t231E/U+s1Jk0IvdD6rLdoi91c3Bmp00rVMPxOjvKmOjgPfE5LESRPMlUli4kJFWxBwbXuhFY+TK2I+BUpiYYKX+4YL3OFrn/EpO4bNcI0NHelbWGqZg57x7rNe9Q==</Modulus></RSAKeyValue>"
                };
            });
            services.Configure<DapiRedGreenQueueProxyOptions>(cfg => cfg.Partition = "IOTNXT.DEFAULT");

            var sp = services.BuildServiceProvider();
            sp.GetService<ExampleCustomFunction>();

            do
            {
                Console.WriteLine("Type EXIT to close");
            } while (Console.ReadLine()?.ToUpper() != "EXIT");
        }
    }
}

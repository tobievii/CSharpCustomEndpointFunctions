using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IoTnxt.DAPI.RedGreenQueue.Abstractions;
using IoTnxt.DAPI.RedGreenQueue.Adapter;
using IoTnxt.Gateway.API.Abstractions;
using Microsoft.Extensions.Logging;

namespace IoTnxt.Example.CustomEndpointFunctions
{
    public class ExampleCustomFunction
    {
        private readonly IGatewayApi _gatewayApi;
        private readonly ILogger<ExampleCustomFunction> _logger;
        private readonly IRedGreenQueueAdapter _redq;

        private async Task RunAsync()
        {
            //We register a gateway to send data back into the IoT.nxt platform
            try
            {
                var gw = new Gateway.API.Abstractions.Gateway
                {
                    GatewayId = Program.GatewayId,
                    Secret = Program.SecretKey,
                    Make = "IoT.nxt",
                    Model = "Custom Function Results",
                    FirmwareVersion = "1.0.0",
                    Devices = new Dictionary<string, Device>
                    {
                        ["FUNCTIONRESULTS"] = new Device
                        {
                            DeviceName = "FUNCTIONRESULTS",
                            DeviceType = "FUNCTIONRESULTS",
                            Properties = new Dictionary<string, DeviceProperty>
                            {
                                ["FUNCTIONARESULT"] = new DeviceProperty { PropertyName = "FUNCTIONARESULT" }
                            }
                        }
                    }
                };

                await _gatewayApi.RegisterGatewayFromGatewayAsync(gw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initialization example function gateway");
            }

            //You can listen to the values from any endpoint (or endpoints) using subscribe.
            //You have to subscribe to the entity's PublishRoutingKey to gets its values.
            await _redq.SubscribeAsync("ENDPOINT.1.7fe0f7fc-ecb1-497e-a371-17fc53986afe.NFY",
                "Functions", null,
                queueName: "CustomEndpointFunctions." + Program.GatewayId,
                createQueue: true,
                process: async (r, msg, a, b, c, d) =>
                {
                    //Get the endpoint's value from the packet.
                    var inputvalue = msg["endPoints"].Value<int>("ec957061-cb78-4483-ae5b-b8010aeac2d1");

                    //Calculate our output value
                    var outputvalue = "Input was: " + inputvalue;

                    //Send the result back as a new endpoint
                    try
                    {
                        await _redq.SendGateway1NotificationAsync(
                            "T000000019",
                            Program.GatewayId,
                            DateTime.UtcNow,
                            null,
                            null,
                            DateTime.UtcNow,
                            true,
                            false,
                            ("FUNCTIONRESULTS", "FUNCTIONARESULT", outputvalue));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending result");
                    }

                    return true;
                });
        }

        public ExampleCustomFunction(
            IRedGreenQueueAdapter redq,
            ILogger<ExampleCustomFunction> logger,
            IGatewayApi gatewayApi)
        {
            _gatewayApi = gatewayApi ?? throw new ArgumentNullException(nameof(gatewayApi));
            _redq = redq ?? throw new ArgumentNullException(nameof(redq));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Task.Run(RunAsync);
        }
    }
}

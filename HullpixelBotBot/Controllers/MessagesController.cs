using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using Microsoft.Bot.Builder.Dialogs;

namespace HullpixelBotBot
{

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region Azure IoT Hub code

        #region Message Send

        static ServiceClient serviceClient = null;
        static string connectionString = "HostName=HullPixelbot.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=3l6MBea3c9YyIkWO9JRU6CKGi8DvVI9ILKo79EimgjM=";

        private async static Task SendCloudToDeviceMessageAsync(string host, string message)
        {
            if(serviceClient==null)
                serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            var commandMessage = new Message(Encoding.ASCII.GetBytes(message));

            await serviceClient.SendAsync(host, commandMessage);
        }

        #endregion

        #region Message Receive

        static string iotHubD2cEndpoint = "messages/events";
        static EventHubClient eventHubClient;

        private static async Task ReceiveMessagesFromDeviceAsync(string partition, CancellationToken ct)
        {
            var eventHubReceiver = eventHubClient.GetDefaultConsumerGroup().CreateReceiver(partition, DateTime.UtcNow);
            while (true)
            {
                if (ct.IsCancellationRequested) break;
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
//                Console.WriteLine("Message received. Partition: {0} Data: '{1}'", partition, data);
            }
        }

        static CancellationTokenSource cts = null;

        static void StopReceiving()
        {
            if (cts == null)
                return;

            cts.Cancel();
            cts = null;
        }


        static void StartReceiving()
        {
            if (cts != null)
                return;

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, iotHubD2cEndpoint);

            var d2cPartitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            cts = new CancellationTokenSource();

            var tasks = new List<Task>();

            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition, cts.Token));
            }
            Task.WaitAll(tasks.ToArray());
        }

        #endregion


        #endregion

        static HullPixelbotEvent ActiveEvent = null;

        Activity messageActivity = null;
        ConnectorClient connector;

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (ActiveEvent == null)
                ActiveEvent = new HullPixelbotEvent(
                    eventName:"Name of event displayed in chat window",
                    eventPassword:"password to authenticate manager via Bot chat window", 
                    MQTTconnectionString: "Connection string from Device Manager",
                    protocolGatewayHost: "",
                    robotNames: new RobotID[] {
                        new RobotID("Friendly name for first robot","robot1MQTTname", "red"), // first robot is red - will display red pixel on selection
                        // Add other robots here - match the names to the ones wired into the RobotNetworkClient code 
                        }
                );

            if (activity.Type == ActivityTypes.Message)
            {
                connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                messageActivity = activity;

                string message = await ActiveEvent.ActOnCommand(activity.Text, activity.Conversation.Id);

                Activity reply = activity.CreateReply(message);

                await connector.Conversations.ReplyToActivityAsync(reply);

                messageActivity = null; 
            }
            else
            {
                HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);

            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        public void SendMessage(string message)
        {
            if (messageActivity == null) return;

            Activity heading = messageActivity.CreateReply(message);

            connector.Conversations.SendToConversation(heading);
        }
    }
}
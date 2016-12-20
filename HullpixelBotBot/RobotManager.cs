using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using System.Text;
using System.Threading;

namespace HullpixelBotBot
{
    #region Messaging interface

    #endregion

    #region Robot classes

    public struct RobotID
    {
        public string PublicName;
        public string MQTTName;
        public string PixelColor;

        public RobotID(string publicName, string MQTTName, string pixelColor)
        {
            this.PublicName = publicName;
            this.MQTTName = MQTTName;
            this.PixelColor = pixelColor;
        }
    }

    public enum RobotState
    {
        Not_In_Use,
        Awaiting_OwnerName,
        Direct_Control,
        Loading_Program,
        Running_Program,
        Group_Program_Running,
        Program_Interrupted
    }

    public enum RobotSendState
    {
        Sent_OK,
        Send_Failed
    }

    public enum RobotStatementResult
    {
        Statement_Transmitted,
        End_Of_Program
    }

    public class RobotDetails
    {
        private HullPixelbotEvent Event;

        private RobotID Id;

        public string MQTTName
        {
            get
            {
                return Id.MQTTName;
            }
        }

        public string PublicName
        {
            get
            {
                return Id.PublicName;
            }
        }

        public string OwnerName { get; set; }

        public string Color
        {
            get
            {
                return Id.PixelColor;
            }
        }

        public RobotState State { get; set; }

        private Stack<RobotState> stateStack = new Stack<RobotState>();

        public void PushState()
        {
            stateStack.Push(State);
        }

        public void PopState()
        {
            State = stateStack.Pop();
        }

        private List<String> programStatements { get; set; }

        public int ProgramSize
        {
            get
            {
                return programStatements.Count;
            }
        }

        public RobotDetails(RobotID Id, HullPixelbotEvent Event)
        {
            this.Id = Id;
            this.Event = Event;
            State = RobotState.Not_In_Use;
            OwnerName = "";
            programStatements = new List<string>();
        }

        int programPos = 0;

        public void ClearProgram()
        {
            programStatements.Clear();
            programPos = 0;
        }

        public void AddProgramStatement(string statement)
        {
            programStatements.Add(statement);
        }

        public string ProcessProgrammingCommand(string command)
        {
            StringBuilder result = new StringBuilder();


            return result.ToString();
        }

        public string GetProgramListingInMarkdown()
        {
            StringBuilder result = new StringBuilder();

            int count = 1;

            foreach (string command in programStatements)
            {
                result.AppendFormat("{0,3:0} ", count++);
                result.AppendLine(command);
                result.AppendLine();
                result.AppendLine();
            }
            return result.ToString();
        }

        public void StartProgram()
        {
            programPos = 0;
        }

        public string CurrentProgramStatement()
        {
            if (programPos < programStatements.Count)
                return programStatements[programPos];
            else
                return null;
        }

        public void StepProgram()
        {
            programPos++;
        }

        public async Task<RobotStatementResult> ObeyStatement()
        {
            if (programPos >= programStatements.Count)
                return RobotStatementResult.End_Of_Program;

            string robotCommand = HullPixelbotCode.DecodeRobotCommand(programStatements[programPos]);

            await Event.SendToRobot(Id.MQTTName, robotCommand);

            programPos++;

            return RobotStatementResult.Statement_Transmitted;
        }
    }

    #endregion

    #region Command decode

    delegate Task<string> CommandAction(string command, object tag);

    class SimpleCommand
    {
        public CommandAction commandAction { get; set; }

        public string CommandText { get; set; }

        public string CommandHelp { get; set; }

        public SimpleCommand(string commandText, string commandHelp, CommandAction commandAction)
        {
            this.CommandText = commandText;
            this.CommandHelp = commandHelp;
            this.commandAction = commandAction;
        }
    }

    class CommandMenu
    {
        private string menuTitle;
        private string helpPrefix;

        private Dictionary<string, SimpleCommand> commandDictionary;

        public string MenuTitle
        {
            get
            {
                return menuTitle;
            }
        }

        public string GetHelpInMarkdown
        {
            get
            {
                StringBuilder result = new StringBuilder();
                result.AppendLine(MenuTitle);
                result.AppendLine();
                result.AppendLine(helpPrefix);
                result.AppendLine();
                foreach (SimpleCommand command in commandDictionary.Values)
                {
                    result.Append(command.CommandText);
                    result.Append(" - ");
                    result.AppendLine(command.CommandHelp);
                    result.AppendLine();
                }
                return result.ToString();
            }
        }


        public async Task<string> ActOnCommand(string commandText, object tag)
        {
            string[] commandwords = commandText.Split(new char[] { ' ', ',' });

            string commandLower = commandwords[0].ToLower();

            if (commandLower == "help")
            {
                return GetHelpInMarkdown;
            }

            if (commandDictionary.ContainsKey(commandLower))
            {
                return await commandDictionary[commandLower].commandAction(command: commandText, tag: tag);
            }
            else
            {
                return "Command " + commandText + " was not found.\n\n Type help for commands.";
            }
        }

        public CommandMenu(string menuTitle, string helpPrefix, SimpleCommand[] commands)
        {
            commandDictionary = new Dictionary<string, SimpleCommand>();

            foreach (SimpleCommand command in commands)
            {
                commandDictionary.Add(command.CommandText.ToLower(), command);
            }

            this.menuTitle = menuTitle;
            this.helpPrefix = helpPrefix;
        }
    }

    #endregion

    #region Robot control code

    public enum RobotColors
    {
        red, green, blue, yellow, magenta, cyan, white, black, lime
    }

    static class HullPixelbotCode
    {

        public static string DecodeRobotCommand(string command)
        {
            string result = command;

            switch (command.ToLower())
            {
                case "forward":
                    result = "MF150";
                    break;
                case "back":
                    result = "MF-150";
                    break;
                case "left":
                    result = "MR-93";
                    break;
                case "right":
                    result = "MR93";
                    break;
                case "red":
                    result = "PC255,0,0";
                    break;
                case "green":
                    result = "PC0,255,0";
                    break;
                case "blue":
                    result = "PC0,0,255";
                    break;
                case "yellow":
                    result = "PC255,255,0";
                    break;
                case "magenta":
                    result = "PC255,0,255";
                    break;
                case "cyan":
                    result = "PC0,255,255";
                    break;
                case "lime":
                    result = "PC50,205,20";
                    break;
                case "violet":
                    result = "PC128,0,128";
                    break;
                case "white":
                    result = "PC255,255,255";
                    break;
                case "black":
                    result = "PC0,0,0";
                    break;
            }
            return result;
        }
    }

    #endregion

    public enum EventState
    {
        NoManager,
        Manager_Entering_Password,
        Event_Open
    }

    public class HullPixelbotEvent
    {
        private string eventName { get; set; }

        private Dictionary<string,RobotDetails> eventRobots { get; set; }

        private string eventPassword;

        private string iotHubConnectionString { get; set; }

        private RobotID[] robotNames;

        private EventState state;

        private string eventManagerID = "";
        public HullPixelbotEvent(string eventName, string eventPassword, string MQTTconnectionString, string protocolGatewayHost, RobotID[] robotNames)
        {
            this.registryManager = RegistryManager.CreateFromConnectionString(MQTTconnectionString);

            this.eventName = eventName;
            this.eventPassword = eventPassword;
            this.iotHubConnectionString = MQTTconnectionString;
            this.protocolGatewayHostName = protocolGatewayHost;
            this.robotNames = robotNames;

            this.listOfDevices = new List<DeviceEntity>();

            state = EventState.NoManager;

            eventRobots = new Dictionary<string,RobotDetails>();

            foreach (RobotID id in robotNames)
            {
                RobotDetails newRobot = new RobotDetails(Id: id, Event: this);
                eventRobots.Add(id.MQTTName, newRobot);
            }
        }

        #region Event State management

        public string EventStateAsMarkup
        {
            get
            {
                string result = "";
                switch (state)
                {
                    case EventState.NoManager:
                        result = "Manager not logged in";
                        break;
                    case EventState.Manager_Entering_Password:
                        result = "Manager entering password";
                        break;
                    case EventState.Event_Open:
                        result = "Event open";
                        break;
                }
                return result;
            }
        }


        #endregion

        #region Command processors

        #region Manager command helper methods

        #region Event control methods

        private async Task<string> openEventAndAssignManager(string userID)
        {
            eventManagerID = userID;

            StringBuilder reply = new StringBuilder();

            reply.AppendLine(await openEvent());

            reply.AppendLine();
            reply.AppendLine("Manager logged in.\n");

            reply.AppendLine("Type help for details of management commands.\n");
            return reply.ToString();
        }


        private async Task<string> openEvent()
        {
            StringBuilder reply = new StringBuilder();

            state = EventState.Event_Open;

            reply.AppendLine("# HullPixelbots at " + eventName);
            reply.AppendLine();
            reply.AppendLine("## Event open");
            reply.AppendLine();
            reply.AppendLine(await resetSession());
            reply.AppendLine();
            return reply.ToString();
        }

        private async Task<string> resetSession()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("## New Session");
            result.AppendLine();
            result.AppendLine("Removing users from robots.");
            result.AppendLine();

            ClearUsers();

            result.AppendLine("Resetting Robots.");
            result.AppendLine();


            string broadcastResult = await broadcastCommand("black");

            result.AppendLine(broadcastResult);
            result.AppendLine();

            result.AppendLine("Robots reset.");
            result.AppendLine();

            return result.ToString();
        }


        private async Task<string> runAllRobots()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("## Run all robots");
            result.AppendLine();

            Task programTask;

            programTask = Task.Factory.StartNew(
                               async () =>
                               {
                                   string runResult = await executeAllRobotPrograms();
                               }
                               );

            return result.ToString();
        }

        private async Task<string> listRobotStatus()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("## Robot assignments");
            result.AppendLine();

            foreach (RobotDetails robot in eventRobots.Values)
            {
                switch (robot.State)
                {
                    case RobotState.Not_In_Use:
                        result.AppendLine("  " + robot.PublicName + " not in use.");
                        break;
                    case RobotState.Awaiting_OwnerName:
                        result.AppendLine("  " + robot.PublicName + " awaiting owner name.");
                        break;
                    case RobotState.Direct_Control:
                        result.AppendLine("  " + robot.PublicName + " being driven by " + robot.OwnerName + ".");
                        break;
                    case RobotState.Loading_Program:
                        result.AppendLine("  " + robot.PublicName + " being programmed by " + robot.OwnerName + ".");
                        break;
                    case RobotState.Running_Program:
                    case RobotState.Program_Interrupted:
                        result.AppendLine("  " + robot.PublicName + " running program by " + robot.OwnerName + ".");
                        break;
                    case RobotState.Group_Program_Running:
                        result.AppendLine("  " + robot.PublicName + " running group program by " + robot.OwnerName + ".");
                        break;
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        private async Task<string> listMQTTDevices()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("## MQTT Devices");
            result.AppendLine();

            DeviceEntity deviceEntity;
            var devices = await registryManager.GetDevicesAsync(maxCountOfDevices);

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    deviceEntity = new DeviceEntity()
                    {
                        Id = device.Id,
                        ConnectionState = device.ConnectionState.ToString(),
                        ConnectionString = CreateDeviceConnectionString(device),
                        LastActivityTime = device.LastActivityTime,
                        LastConnectionStateUpdatedTime = device.ConnectionStateUpdatedTime,
                        LastStateUpdatedTime = device.StatusUpdatedTime,
                        MessageCount = device.CloudToDeviceMessageCount,
                        State = device.Status.ToString(),
                        SuspensionReason = device.StatusReason
                    };

                    if (device.Authentication != null)
                    {

                        deviceEntity.PrimaryKey = device.Authentication.SymmetricKey?.PrimaryKey;
                        deviceEntity.SecondaryKey = device.Authentication.SymmetricKey?.SecondaryKey;
                        deviceEntity.PrimaryThumbPrint = device.Authentication.X509Thumbprint?.PrimaryThumbprint;
                        deviceEntity.SecondaryThumbPrint = device.Authentication.X509Thumbprint?.SecondaryThumbprint;

                        //if ((device.Authentication.SymmetricKey != null) &&
                        //    !((device.Authentication.SymmetricKey.PrimaryKey == null) ||
                        //      (device.Authentication.SymmetricKey.SecondaryKey == null)))
                        //{
                        //    deviceEntity.PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey;
                        //    deviceEntity.SecondaryKey = device.Authentication.SymmetricKey.SecondaryKey;
                        //    deviceEntity.PrimaryThumbPrint = "";
                        //    deviceEntity.SecondaryThumbPrint = "";
                        //}
                        //else
                        //{
                        //    deviceEntity.PrimaryKey = "";
                        //    deviceEntity.SecondaryKey = "";
                        //    deviceEntity.PrimaryThumbPrint = device.Authentication.X509Thumbprint.PrimaryThumbprint;
                        //    deviceEntity.SecondaryThumbPrint = device.Authentication.X509Thumbprint.SecondaryThumbprint;
                        //}
                    }

                    if (eventRobots.ContainsKey(deviceEntity.Id))
                    {
                        listOfDevices.Add(deviceEntity);
                        result.AppendLine(deviceEntity.StatusString);
                        result.AppendLine();
                    }
                }
            }
            return result.ToString();
        }

        string[] testProgram = new string[]
        {
            "red","green","blue","yellow","black","white","lime","magenta","cyan"
        };

        private async Task<string> setupTestProgram()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("## Setup Test Program");
            result.AppendLine();

            result.AppendLine(await resetSession());
            result.AppendLine();

            int robotCount = 1;

            foreach (RobotDetails robot in eventRobots.Values)
            {
                robot.State = RobotState.Loading_Program;
                robot.OwnerName = robot.MQTTName + " owner";
                result.AppendLine(robot.OwnerName);
                result.AppendLine();
                robot.ClearProgram();
                // add a different number of program strings for each robot
                for (int i = 0; i < robotCount; i++)
                {
                    robot.AddProgramStatement(robot.Color);
                    robot.AddProgramStatement("white");
                    robot.AddProgramStatement(robot.Color);
                    robot.AddProgramStatement("white");
                }
                robotCount++;
            }

            return result.ToString();
        }

        private async Task<string> testEvent(string command)
        {
            StringBuilder result = new StringBuilder();

            string[] commandwords = command.Split(new char[] { ' '});

            string commandLower = commandwords[0].ToLower();

            if (commandwords.Length == 1)
            {
                result.AppendLine("Missing test option");
            }
            else
            {
                switch (commandwords[1].ToLower())
                {
                    case "setup":
                        result.AppendLine(await setupTestProgram());
                        break;
                    case "broadcast":
                        if(commandwords.Length == 2)
                        {
                            result.AppendLine("Missing broadcast command");
                        }
                        else
                        {
                            result.AppendLine(await broadcastCommand(commandwords[2]));
                        }
                        break;

                    default:
                        result.AppendLine("Invalid test command. Commands are setup and broadcast.");
                        break;
                }
            }

            result.AppendLine();

            return result.ToString();
        }

        #endregion

        #region Manager menu methods

        static private async Task<string> resetSessionCommand(string command, object tag)
        {
            HullPixelbotEvent activeEvent = tag as HullPixelbotEvent;

            StringBuilder result = new StringBuilder();
            result.AppendLine("Reset the session and remove all active robots");
            result.AppendLine();

            result.AppendLine(await activeEvent.resetSession());
            result.AppendLine();

            return result.ToString();
        }

        static private async Task<string> runAllRobotsCommand(string command, object tag)
        {
            HullPixelbotEvent activeEvent = tag as HullPixelbotEvent;

            StringBuilder result = new StringBuilder();
            result.AppendLine("Runs all robots simultaneously");
            result.AppendLine();

            result.AppendLine(await activeEvent.runAllRobots());
            result.AppendLine();

            return result.ToString();
        }

        static private async Task<string> listRobotStatusCommand(string command, object tag)
        {
            HullPixelbotEvent activeEvent = tag as HullPixelbotEvent;

            StringBuilder result = new StringBuilder();

            result.AppendLine(await activeEvent.listRobotStatus());
            result.AppendLine();

            return result.ToString();
        }

        static private async Task<string> listMQTTStatusCommand(string command, object tag)
        {
            HullPixelbotEvent activeEvent = tag as HullPixelbotEvent;

            StringBuilder result = new StringBuilder();

            result.AppendLine(await activeEvent.listMQTTDevices());
            result.AppendLine();

            return result.ToString();
        }

        static private async Task<string> setupDemoCommand(string command, object tag)
        {
            HullPixelbotEvent activeEvent = tag as HullPixelbotEvent;

            StringBuilder result = new StringBuilder();

            result.AppendLine(await activeEvent.testEvent(command));

            result.AppendLine();

            return result.ToString();
        }

        #endregion

        static private CommandMenu managerCommands = new CommandMenu(
             menuTitle: "#Event Management",
             helpPrefix: "These are the managerment commands available",
             commands: new SimpleCommand[]
             {
                 new SimpleCommand("list", "List the robot status information", listRobotStatusCommand),
                 new SimpleCommand("devices", "List the MQTT device information", listMQTTStatusCommand),
                 new SimpleCommand("test", "Test behaviours", setupDemoCommand),
                 new SimpleCommand("run", "Runs the programs in all the robots at the same time", runAllRobotsCommand),
                 new SimpleCommand("restart", "Reset the session and remove all active robots", resetSessionCommand)
             }
            );

        #endregion

        private async Task<string> actOnManagerCommand(string command)
        {
            string lowCommand = command.ToLower();

            StringBuilder reply = new StringBuilder();

            string commandResult = await managerCommands.ActOnCommand(command, this);

            reply.AppendLine(commandResult);

            return reply.ToString();
        }

        #region User Action helper methods

        private async Task<string> GetUserRobot(string userID)
        {
            StringBuilder result = new StringBuilder();

            RobotDetails userRobot = await AssignRobot(userID);

            if (userRobot == null)
            {
                result.AppendLine("I'm sorry, all the robots are in use in this session.");
                result.AppendLine();
                result.AppendLine("Please try again during the next session.");
                result.AppendLine();
            }
            else
            {
                await SendToRobot(MQTTName: userRobot.MQTTName, message: HullPixelbotCode.DecodeRobotCommand(userRobot.Color));
                result.AppendLine("# You've got a robot!");
                result.AppendLine("Your robot is " + userRobot.PublicName + ".");
                result.AppendLine();
                result.AppendLine(" The light on the robot should be showing " + userRobot.Color);
                result.AppendLine();
                result.AppendLine("Grab your robot and say hello.");
                result.AppendLine();
                result.AppendLine("Your robot would like to know who is controlling it");
                result.AppendLine();
                result.AppendLine("Enter your name");
                result.AppendLine();
            }

            return result.ToString();
        }

        #endregion


        System.Threading.CancellationTokenSource programCancellationSource = new System.Threading.CancellationTokenSource();

        float gapBetweenStatements = 4.0f;

        private async Task<string> executeRobotProgram(RobotDetails robot)
        {
            StringBuilder result = new StringBuilder();

            DateTime startTime = DateTime.Now;

            robot.State = RobotState.Running_Program;

            robot.StartProgram();

            while (true)
            {
                RobotStatementResult statementResult;

                statementResult = await robot.ObeyStatement();

                if (statementResult == RobotStatementResult.End_Of_Program)
                    break;

                if (robot.State == RobotState.Program_Interrupted)
                {
                    break;
                }

                using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
                {
                    tmpEvent.WaitOne(TimeSpan.FromMilliseconds(gapBetweenStatements * 1000));
                }
            }

            DateTime endTime = DateTime.Now;

            result.Append("Started at " + startTime.ToShortTimeString());
            result.AppendLine();
            result.AppendLine("Ended at " + endTime.ToShortTimeString());
            result.AppendLine();

            robot.State = RobotState.Loading_Program;

            return result.ToString();
        }

        int Number_Of_Stop_Tries = 3;

        private int countRunningRobotsAndStopThem()
        {
            int result = 0;
            foreach (RobotDetails robot in eventRobots.Values)
            {

                if (robot.State == RobotState.Running_Program || robot.State == RobotState.Program_Interrupted)
                {
                    robot.State = RobotState.Program_Interrupted;
                    result++;
                }
            }
            return result;
        }

        private async Task<string> executeAllRobotPrograms()
        {
            StringBuilder result = new StringBuilder();

            DateTime startTime = DateTime.Now;

            // first stop all the programs from running

            int stopCount;

            for (stopCount = 0; stopCount < Number_Of_Stop_Tries; stopCount++)
            {
                // stop any active robots 
                if (countRunningRobotsAndStopThem() == 0)
                    break;

                using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
                {
                    tmpEvent.WaitOne(TimeSpan.FromMilliseconds(gapBetweenStatements * 2000));
                }
            }

            if (stopCount == Number_Of_Stop_Tries)
            {
                result.AppendLine("Not all robots could be stopped");
            }
            else
            {
                result.AppendLine("Executing programs stopped");
            }

            result.AppendLine();

            List<RobotDetails> activeRobots = new List<RobotDetails>();

            foreach (RobotDetails robot in eventRobots.Values)
            {
                if (robot.ProgramSize == 0)
                    continue;

                robot.PushState();

                robot.State = RobotState.Group_Program_Running;

                activeRobots.Add(robot);

                robot.StartProgram();
            }

            do
            {
                // Work through each robot and perform the next command

                List<RobotDetails> stoppedRobots = new List<RobotDetails>();

                List<RobotMessage> messages = new List<RobotMessage>();

                // spin through the robots and add a list of messages
                foreach (RobotDetails robot in activeRobots)
                {
                    string statement = robot.CurrentProgramStatement();
                    if (statement == null)
                    {
                        stoppedRobots.Add(robot);
                    }
                    else
                    {
                        string robotCommand = HullPixelbotCode.DecodeRobotCommand(statement);
                        messages.Add(new RobotMessage(robot.MQTTName, robotCommand));
                        robot.StepProgram();
                    }
                }

                RobotSendState sendResult = await BulkSendMessages(messages);

                foreach (RobotDetails robot in stoppedRobots)
                {
                    robot.PopState();
                    activeRobots.Remove(robot);
                }

                using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
                {
                    tmpEvent.WaitOne(TimeSpan.FromMilliseconds(gapBetweenStatements * 1000));
                }

            } while (activeRobots.Count>0);

            DateTime endTime = DateTime.Now;

            result.Append("Started at " + startTime.ToShortTimeString());
            result.AppendLine();
            result.AppendLine("Ended at " + endTime.ToShortTimeString());
            result.AppendLine();

            return result.ToString();
        }


        private async Task<string> runRobotProgram(RobotDetails robot)
        {
            Task programTask;

            programTask = Task.Factory.StartNew(
                               async () =>
                               {
                                   string result = await executeRobotProgram(robot);
                               }
                               );

            return "running";
        }

        private async Task<string> releaseRobot(RobotDetails robot)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine(robot.PublicName + " has been returned to the wild");
            result.AppendLine();
            robot.State = RobotState.Not_In_Use;
            await SendToRobot(MQTTName: robot.MQTTName, message: HullPixelbotCode.DecodeRobotCommand("black"));
            return result.ToString();
        }


        private async Task<string> actOnUserCommand(string command, string userID)
        {
            RobotDetails userRobot = GetRobotForUser(userID);

            if (userRobot == null)
            {
                /// Need to assign a robot to the user
                /// This will either work - in which case we want the user to enter their name
                /// before we can continue or it will fail, in which case we can't
                /// go any further anyway
                string getResult = await GetUserRobot(userID);
                return getResult;
            }

            StringBuilder result = new StringBuilder();
            string lowCommand = command.ToLower();

            switch (userRobot.State)
            {
                case RobotState.Awaiting_OwnerName:
                    userRobot.OwnerName = command;
                    result.AppendLine("Thanks for entering your name.");
                    result.AppendLine();
                    result.AppendLine("Now start entering robot commands to drive your robot " + userRobot.PublicName);
                    result.AppendLine();
                    result.AppendLine("Type help to find out what commands are available.");
                    result.AppendLine();
                    result.AppendLine();
                    userRobot.State = RobotState.Direct_Control;
                    break;

                case RobotState.Direct_Control:
                    // Already got a robot - since we are in direct control just send the command

                    switch(lowCommand)
                    {

                        case "program":
                            // switching to program mode for the robot
                            userRobot.State = RobotState.Loading_Program;
                            result.AppendLine("You are now programming " + userRobot.PublicName);
                            result.AppendLine();
                            result.AppendLine("Enter robot commands in the order you want them to be obeyed.");
                            result.AppendLine();
                            result.AppendLine("Type help for a list of program commands.");
                            result.AppendLine();
                            break;

                        case "bye":
                            result.AppendLine(await releaseRobot(userRobot));
                            result.AppendLine();
                            break;

                        case "help":
                            result.AppendLine("## Robot commands");
                            result.AppendLine();
                            result.AppendLine("forward - move forward one square");
                            result.AppendLine();
                            result.AppendLine("back - move ack on square");
                            result.AppendLine();
                            result.AppendLine("left - turn 90 degrees left");
                            result.AppendLine();
                            result.AppendLine("right - turn 90 degrees right");
                            result.AppendLine();
                            result.AppendLine("colourname - set the pixel that colour");
                            result.AppendLine();
                            result.AppendLine("program - switch to program mode");
                            result.AppendLine();
                            result.AppendLine("bye - end your session and release your robot");
                            result.AppendLine();
                            break;

                        default:
                            await SendToRobot(MQTTName: userRobot.MQTTName, message: HullPixelbotCode.DecodeRobotCommand(command));
                            result.AppendLine("Your command has been sent directly to " + userRobot.PublicName + ".");
                            break;
                    }

                    break;

                case RobotState.Loading_Program:
                    // Add the command to the ones already stored
                    switch (lowCommand)
                    {
                        case "direct":
                            // switching to program mode for the robot
                            userRobot.State = RobotState.Direct_Control;
                            result.AppendLine("You are now directly controlling " + userRobot.PublicName);
                            result.AppendLine();
                            result.AppendLine("Enter robot commands to have them obeyed instantly.");
                            result.AppendLine();
                            result.AppendLine("Type help for a list of robot commands.");
                            result.AppendLine();
                            break;

                        case "list":
                            result.AppendLine("This is what " + userRobot.PublicName + " is going to do:");
                            result.AppendLine();
                            string listing = userRobot.GetProgramListingInMarkdown();
                            result.AppendLine(listing);
                            result.AppendLine();
                            break;

                        case "new":
                            userRobot.ClearProgram();
                            result.AppendLine("You have cleared the program in " + userRobot.PublicName);
                            result.AppendLine();
                            break;

                        case "run":
                            // run the program 
                            string runMessage = await runRobotProgram(userRobot);
                            result.AppendLine(runMessage);
                            break;

                        case "bye":
                            result.AppendLine(await releaseRobot(userRobot));
                            result.AppendLine();
                            break;

                        case "help":
                            result.AppendLine("## Program commands");
                            result.AppendLine();
                            result.AppendLine("Any move command is added to the program after the last one");
                            result.AppendLine();
                            result.AppendLine("list - list the program");
                            result.AppendLine();
                            result.AppendLine("clear - clear the program");
                            result.AppendLine();
                            result.AppendLine("run - run the program");
                            result.AppendLine();
                            result.AppendLine("direct - switch to direct control mode");
                            result.AppendLine();
                            result.AppendLine("bye - end your session and release your robot");
                            result.AppendLine();
                            break;

                        default:
                            result.AppendLine("The instruction " + command + " has been added to the program.");
                            result.AppendLine();
                            userRobot.AddProgramStatement(command);
                            break;
                    }

                    break;

                case RobotState.Running_Program:
                case RobotState.Program_Interrupted:
                    result.AppendLine("You can't input commands while the robot is running a program.");
                    result.AppendLine();
                    result.AppendLine("Wait for the program to finish before entering commands.");
                    result.AppendLine();
                    break;

            }
            return result.ToString();
        }

        /// <summary>
        /// Endpoint for all commands to drive the event. Receive commands and a user ID and act on them 
        /// depending on the context of the command
        /// </summary>
        /// <param name="command">command to be acted on</param>
        /// <param name="userID">userid for the user issuing the command</param>
        /// <returns>Markdown formatted string containing response to be sent to the user</returns>
        public async Task<string> ActOnCommand(string command, string userID)
        {
            string lowCommand = command.ToLower();

            StringBuilder reply = new StringBuilder();

            switch (state)
            {
                case EventState.NoManager:
                    // Event not currently active
                    reply.AppendLine("# HullPixelbot at " + eventName);

                    // In this state the event is not open and the manager needs to login
                    // There is no proper security, but it will work for now

                    if (lowCommand == "login")
                    {
                        reply.AppendLine("Please enter the event password to continue.\n");
                        state = EventState.Manager_Entering_Password;
                    }
                    else
                    {
                        reply.AppendLine("This event is not active at the moment.");
                        reply.AppendLine();
                        reply.AppendLine("If you are the manager, please log in using the login command");
                        reply.AppendLine();
                    }
                    break;

                case EventState.Manager_Entering_Password:
                    // Getting the manager password
                    if (command == eventPassword)
                    {
                        reply.AppendLine(await openEventAndAssignManager(userID) + "\n");
                    }
                    else
                    {
                        reply.AppendLine("That's not what I thought the password was.");
                        reply.AppendLine();
                        reply.AppendLine("Please Log in using the login command.\n");
                        reply.AppendLine();
                        state = EventState.NoManager;
                    }
                    break;

                case EventState.Event_Open:
                    // Event is open - see if it is a manager command
                    if (userID == eventManagerID)
                    {
                        string managerAction = await actOnManagerCommand(command);
                        reply.AppendLine(managerAction);
                    }
                    else
                    {
                        string userAction = await actOnUserCommand(command: command, userID: userID);
                        reply.AppendLine(userAction);
                    }
                    break;
            }
            return reply.ToString();
        }

        #endregion

        #region Assigning Robots to users

        private Dictionary<string, RobotDetails> userRobots = new Dictionary<string, RobotDetails>();

        public void ClearUsers()
        {
            userRobots.Clear();

            foreach (RobotDetails robot in eventRobots.Values)
            {
                robot.State = RobotState.Not_In_Use;
                robot.ClearProgram();
            }
        }

        public RobotDetails GetRobotForUser(string id)
        {
            if (userRobots.ContainsKey(id))
            {
                return userRobots[id];
            }
            return null;
        }

        public async Task<RobotDetails> AssignRobot(string id)
        {
            RobotDetails result = null;

            DeviceEntity deviceEntity;
            var devices = await registryManager.GetDevicesAsync(maxCountOfDevices);

            // If we can't find out what's available, give up immediately

            if (devices == null)
            {
                result = null;
            }

            else
            {
                // Spin through each device found

                foreach (var device in devices)
                {
                    // is the device live?

                    if (device.ConnectionState.ToString().ToLower() == "connected")
                    {
                        // is it a robot on this event?

                        if (eventRobots.ContainsKey(device.Id))
                        {
                            // Is the robot already assigned?
                            RobotDetails robot = eventRobots[device.Id];
                            if (robot.State == RobotState.Not_In_Use)
                            {
                                robot.State = RobotState.Awaiting_OwnerName;
                                robot.OwnerName = "";
                                result = robot;
                                userRobots.Add(id, result);
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region MQTT management

        public class DeviceEntity : IComparable<DeviceEntity>
        {
            public string Id { get; set; }
            public string PrimaryKey { get; set; }
            public string SecondaryKey { get; set; }
            public string PrimaryThumbPrint { get; set; }
            public string SecondaryThumbPrint { get; set; }
            public string ConnectionString { get; set; }
            public string ConnectionState { get; set; }
            public DateTime LastActivityTime { get; set; }
            public DateTime LastConnectionStateUpdatedTime { get; set; }
            public DateTime LastStateUpdatedTime { get; set; }
            public int MessageCount { get; set; }
            public string State { get; set; }
            public string SuspensionReason { get; set; }

            public int CompareTo(DeviceEntity other)
            {
                return string.Compare(this.Id, other.Id, StringComparison.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                return $"Device ID = {this.Id}, Primary Key = {this.PrimaryKey}, Secondary Key = {this.SecondaryKey}, Primary Thumbprint = {this.PrimaryThumbPrint}, Secondary Thumbprint = {this.SecondaryThumbPrint}, ConnectionString = {this.ConnectionString}, ConnState = {this.ConnectionState}, ActivityTime = {this.LastActivityTime}, LastConnState = {this.LastConnectionStateUpdatedTime}, LastStateUpdatedTime = {this.LastStateUpdatedTime}, MessageCount = {this.MessageCount}, State = {this.State}, SuspensionReason = {this.SuspensionReason}\r\n";
            }

            public string StatusString
            {
                get
                {
                    return $"{this.Id} Status = {this.ConnectionState}";
                }
            }
        }

        private String CreateDeviceConnectionString(Device device)
        {
            StringBuilder deviceConnectionString = new StringBuilder();

            var hostName = String.Empty;
            var tokenArray = iotHubConnectionString.Split(';');
            for (int i = 0; i < tokenArray.Length; i++)
            {
                var keyValueArray = tokenArray[i].Split('=');
                if (keyValueArray[0] == "HostName")
                {
                    hostName = tokenArray[i] + ';';
                    break;
                }
            }

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                deviceConnectionString.Append(hostName);
                deviceConnectionString.AppendFormat("DeviceId={0}", device.Id);

                if (device.Authentication != null)
                {
                    if ((device.Authentication.SymmetricKey != null) && (device.Authentication.SymmetricKey.PrimaryKey != null))
                    {
                        deviceConnectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
                    }
                    else
                    {
                        deviceConnectionString.AppendFormat(";x509=true");
                    }
                }

                if (this.protocolGatewayHostName.Length > 0)
                {
                    deviceConnectionString.AppendFormat(";GatewayHostName=ssl://{0}:8883", this.protocolGatewayHostName);
                }
            }

            return deviceConnectionString.ToString();
        }

        int maxCountOfDevices = 1000;
        private String protocolGatewayHostName;

        private RegistryManager registryManager;

        private List<DeviceEntity> listOfDevices;

        public async Task<List<DeviceEntity>> GetDevices()
        {
            listOfDevices.Clear();

            try
            {
                DeviceEntity deviceEntity;
                var devices = await registryManager.GetDevicesAsync(maxCountOfDevices);

                if (devices != null)
                {
                    foreach (var device in devices)
                    {
                        deviceEntity = new DeviceEntity()
                        {
                            Id = device.Id,
                            ConnectionState = device.ConnectionState.ToString(),
                            ConnectionString = CreateDeviceConnectionString(device),
                            LastActivityTime = device.LastActivityTime,
                            LastConnectionStateUpdatedTime = device.ConnectionStateUpdatedTime,
                            LastStateUpdatedTime = device.StatusUpdatedTime,
                            MessageCount = device.CloudToDeviceMessageCount,
                            State = device.Status.ToString(),
                            SuspensionReason = device.StatusReason
                        };

                        if (device.Authentication != null)
                        {

                            deviceEntity.PrimaryKey = device.Authentication.SymmetricKey?.PrimaryKey;
                            deviceEntity.SecondaryKey = device.Authentication.SymmetricKey?.SecondaryKey;
                            deviceEntity.PrimaryThumbPrint = device.Authentication.X509Thumbprint?.PrimaryThumbprint;
                            deviceEntity.SecondaryThumbPrint = device.Authentication.X509Thumbprint?.SecondaryThumbprint;
                        }

                        listOfDevices.Add(deviceEntity);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return listOfDevices;
        }


        #region Robot message sending


        public async Task OldSendToRobot(string MQTTName, string message)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            var serviceMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            serviceMessage.Ack = DeliveryAcknowledgement.Full;
            serviceMessage.MessageId = Guid.NewGuid().ToString();

            await serviceClient.SendAsync(MQTTName, serviceMessage);

            await serviceClient.CloseAsync();
        }

        public async Task SendToRobot(string MQTTName, string message)
        {
            Task programTask;

            programTask = Task.Factory.StartNew(
                async () =>
                {
                    ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

                    var serviceMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
                    serviceMessage.Ack = DeliveryAcknowledgement.Full;
                    serviceMessage.MessageId = Guid.NewGuid().ToString();

                    await serviceClient.SendAsync(MQTTName, serviceMessage);

                    await serviceClient.CloseAsync();
                }
                );
        }

        public async Task<RobotSendState> OldBroadcastRobotMessage(string MessageToSend)
        {
            RobotSendState result = RobotSendState.Sent_OK;

            foreach (RobotDetails robot in eventRobots.Values)
            {
                Task programTask;

                programTask = Task.Factory.StartNew(
                                   async () =>
                                   {
                                       await SendToRobot(robot.MQTTName, MessageToSend);
                                   }
                                   );
            }

            return result;
        }

        public struct RobotMessage
        {
            public string RobotName { get; set; }
            public string Message { get; set; }

            public RobotMessage(string robotName, string message)
            {
                this.RobotName = robotName;
                this.Message = message;
            }
        }

        public async Task<RobotSendState> BulkSendMessages ( List<RobotMessage> messages)
        {
            try
            {
                ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

                foreach (RobotMessage message in messages)
                {
                    var serviceMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message.Message));
                    serviceMessage.Ack = DeliveryAcknowledgement.Full;
                    serviceMessage.MessageId = Guid.NewGuid().ToString();

                    await serviceClient.SendAsync(message.RobotName, serviceMessage);
                }

                await serviceClient.CloseAsync();
            }
            catch
            {
                return RobotSendState.Send_Failed;
            }

            return RobotSendState.Sent_OK;
        }
        private async Task<string> broadcastCommand(string command)
        {
            StringBuilder result = new StringBuilder();

            string robotCommand = HullPixelbotCode.DecodeRobotCommand(command);

            List<RobotMessage> messages = new List<RobotMessage>();

            foreach (RobotDetails robot in eventRobots.Values)
            {
                messages.Add(new RobotMessage(robot.MQTTName, robotCommand));
            }

            RobotSendState sendResult = await BulkSendMessages(messages);

            result.AppendLine(sendResult.ToString());
            result.AppendLine();

            return result.ToString();
        }

        //public async Task<RobotSendState> BroadcastRobotMessage(string MessageToSend)
        //{
        //    RobotSendState result = RobotSendState.Sent_OK;

        //    foreach (RobotDetails robot in eventRobots.Values)
        //    {
        //        await SendToRobot(robot.MQTTName, MessageToSend);
        //    }

        //    return result;
        //}

        #endregion

        #endregion

    }
}
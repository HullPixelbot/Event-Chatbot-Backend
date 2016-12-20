using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace HullpixelBotBot
{
    public class RobotResponse
    {
        public int version { get; set; }
        public List<int> distance { get; set; }
        public List<int> lightLevel { get; set; }
        public int leftSpeed { get; set; }
        public int rightSpeed { get; set; }

        public static string getJson()
        {
            string result;

            RobotResponse response = new RobotResponse();

            response.version = 1;

            response.distance = new List<int>();
            response.distance.Add(100);

            response.lightLevel = new List<int>();
            response.lightLevel.Add(200);
            response.lightLevel.Add(300);
            response.lightLevel.Add(400);

            response.leftSpeed = 50;
            response.rightSpeed = -50;

            result = JsonConvert.SerializeObject(response);

            return result;
        }
    }

    public class LightResponse
    {
        public int version { get; set; }
        public int light { get; set; }
        public int button { get; set; }

        public static string getJson()
        {
            string result;

            LightResponse response = new LightResponse();

            response.version = 1;
            response.light = 1;
            response.button = 1;

            result = JsonConvert.SerializeObject(response);

            return result;
        }
    }

}
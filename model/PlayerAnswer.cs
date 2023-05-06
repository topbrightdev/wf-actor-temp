using System;
using Newtonsoft.Json;

namespace showdown.model
{
    public class PlayerAnswer
    {
        public string playerID;
        public int questionID;
        public int choice;
        public float time;
    }
}

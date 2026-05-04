using System;
using System.Collections.Generic;
using System.Text;

namespace TheFeed
{
    public class Config
    {
        public int Port { get; set; } = 8080;
        public string TopDir { get; set; }

        public string inspiretext { get; set; }
        public string Title { get; set; } = "TheFeed";
    }
}

namespace CJablotron
{
    using System;

    public class Message
    {
        private DateTime date = DateTime.Now;
        private string eventId;
        private int objectId;
        private int partNo;
        private string pultId;
        private string source;
        private int zoneNo;

        public DateTime Date
        {
            get
            {
                return this.date;
            }
            set
            {
                this.date = value;
            }
        }

        public string EventId
        {
            get
            {
                return this.eventId;
            }
            set
            {
                this.eventId = value;
            }
        }

        public int ObjectId
        {
            get
            {
                return this.objectId;
            }
            set
            {
                this.objectId = value;
            }
        }

        public int PartNo
        {
            get
            {
                return this.partNo;
            }
            set
            {
                this.partNo = value;
            }
        }

        public string PultId
        {
            get
            {
                return this.pultId;
            }
            set
            {
                this.pultId = value;
            }
        }

        public string Source
        {
            get
            {
                return this.source;
            }
            set
            {
                this.source = value;
            }
        }

        public int ZoneNo
        {
            get
            {
                return this.zoneNo;
            }
            set
            {
                this.zoneNo = value;
            }
        }
    }
}


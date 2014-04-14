using System;

namespace CJablotron
{
    /// <summary>
    /// Класс сообщения для хранения информации о сообщении на пульт.
    /// </summary>
    public class Message
    {
        private DateTime date = DateTime.Now;
        private string eventId;
        private int objectId;
        private int partNo;
        private string pultId;
        private string source;
        private int zoneNo;

        /// <summary>
        /// Дата сообщения.
        /// </summary>
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

        /// <summary>
        /// Идентификатор события.
        /// </summary>
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

        /// <summary>
        /// Идентификатор объекта.
        /// </summary>
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

        /// <summary>
        /// Номер раздела.
        /// </summary>
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

        /// <summary>
        /// Идентификатор пульта.
        /// </summary>
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

        /// <summary>
        /// Описание источника события.
        /// </summary>
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

        /// <summary>
        /// Номер зоны.
        /// </summary>
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


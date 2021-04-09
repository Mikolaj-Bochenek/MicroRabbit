using System;

namespace MicroRabbit.Domain.Core.Events
{
    public abstract class Event
    {
        protected Event() => TimeStamp = DateTime.Now;
        
        public DateTime TimeStamp { get; protected set; }
    }
}
using System;
using MicroRabbit.Domain.Core.Events;

namespace MicroRabbit.Domain.Core.Commands
{
    public abstract class Command : Message
    {
        protected Command() => TimeStamp = DateTime.Now;
        
        public DateTime TimeStamp { get; protected set; }
    }
}
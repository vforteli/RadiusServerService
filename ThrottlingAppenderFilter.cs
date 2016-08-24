using log4net.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace Flexinets.Radius
{
    public class DuplicateMessageThrottleFilter : FilterSkeleton
    {
        private Int32 repeatThreahold = 20;
        private String previousMessage = "";
        private Int32 repeatCount = 0;

        public override void ActivateOptions()
        {
            base.ActivateOptions();
        }

        public override FilterDecision Decide(LoggingEvent loggingEvent)
        {
            if (loggingEvent.LoggerName == "Flexinets.Radius.MobileDataPacketHandler")
            {
                var message = loggingEvent.MessageObject.ToString();

                if (previousMessage.Equals(message) && repeatCount < repeatThreahold)
                {
                    repeatCount++;
                    return FilterDecision.Deny;
                }




                previousMessage = message;
                repeatCount = 0;
                return FilterDecision.Accept;
            }

            return FilterDecision.Neutral;
        }
    }
}

using System;

namespace despBuffers
{
    public class BuffersException : Exception
    {
        public BuffersException()
        {
        }
        
        public BuffersException(string message) :
            base(message)
        {
        }

        public static void ExecuteNoExceptions(Action a)
        {
            try
            {
                a.Invoke();
            }
            catch
            {
                // ignored
            }
        }
    }
}


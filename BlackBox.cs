using System;
using System.Collections.Generic;
using System.Text;


namespace flightmanagementcomputer
{
    public class BlackBox : EventArgs
    {
        string name; 
        public List<ChangeEvent> history = new List<ChangeEvent>();
        public event EventHandler<ChangeEvent> RaiseChangeEvent;

        public BlackBox(string name)
        {
            this.name = name;
        }
        public void addEvent(ChangeEvent e)
        {
            history.Add(e);
            raiseChangeEvent(e);
            //Console.WriteLine(name + " " + e.ToString());
        }

        public string getName()
        {
            return name;
        }

        public List<ChangeEvent> getHistory()
        {
            return history;
        }

        protected virtual void raiseChangeEvent(ChangeEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            RaiseChangeEvent?.Invoke(this, e);
            //Console.WriteLine("New Event:" + e.ToString());
        }
        public void dumpHistory()
        {
            Console.WriteLine("=========FLIGHT HISTORY==========");
            foreach (ChangeEvent evnt in history)
            {
                Console.WriteLine(evnt.ToString());
            }

        }

    }
}

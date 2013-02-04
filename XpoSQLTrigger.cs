using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using System.Timers;
using System.Threading;

namespace SOM3.Classes.XpoSqlTriggers
{
    class XpoSQLTriggerInfo : XPObject
    {
        public XpoSQLTriggerInfo(Session session) : base(session) { }
        public XpoSQLTriggerInfo() : base() { }

        public string triggerName;
        public DateTime? timestamp;
    }

    /// <summary>
    /// Database update trigger
    /// </summary>
    public class SQLTriggerEvent : EventArgs
    {
        public string name { get; set; }
        public DateTime? timestamp { get; set; }
    }

    public class XpoTrigger{

        public delegate void SQLTriggerHandler(Object sender, SQLTriggerEvent e);
        public event SQLTriggerHandler sqlTriggerEvent;        
        static Session UpdateSession = new Session();        
        static Session TimeSession = new Session();

        List<SQLTriggerEvent> triggerList = new List<SQLTriggerEvent>();
        System.Timers.Timer timer = new System.Timers.Timer();
        private SynchronizationContext context;
        
        public static Object updateLock = new Object();        
        public static Object timeLock = new Object();

        /// <summary>
        /// Create a new instance of the XpoTrigger Class
        /// </summary>
        /// <param name="time">Time in ms to refresh to look for changes (Default is 1000ms)</param>
        public XpoTrigger(double time = 1000)
        {
            UpdateSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
            UpdateSession.LockingOption = LockingOption.None;


            TimeSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
            TimeSession.LockingOption = LockingOption.None;

            context = SynchronizationContext.Current;
            if (context == null)
            {
                context = new SynchronizationContext();
            }
            timer.Interval = time;
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // check all registered permissions...
            DateTime? temp;

            foreach (SQLTriggerEvent trigger in triggerList)
            {
                if ((temp = getSQLTriggerTime(trigger.name)) != trigger.timestamp)
                {
                    trigger.timestamp = temp;

                    SQLTriggerEvent evnt = new SQLTriggerEvent();
                    evnt.name = trigger.name;
                    evnt.timestamp = temp;

                    context.Post(new SendOrPostCallback(delegate(object state)
                    {
                        SQLTriggerHandler handler = sqlTriggerEvent;

                        if (handler != null)
                        {
                            handler(this, evnt);
                        }
                    }), null);
                }                 
            }
        }

        /// <summary>
        /// Register new event
        /// </summary>
        /// <param name="name">Event Name</param>
        public static void register(string name)
        {
            if (!UpdateSession.IsConnected)
            {
                UpdateSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
                UpdateSession.LockingOption = LockingOption.None;
                UpdateSession.Connect();
            }
            XpoSQLTriggerInfo trigger;
         
                trigger = UpdateSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
         
            if (trigger == null)
            {
                trigger = new XpoSQLTriggerInfo(UpdateSession);
                trigger.triggerName = name;
                lock (updateLock)
                {
                    trigger.timestamp = (DateTime)UpdateSession.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);            
                    trigger.Save();
                }
            }

        }

        /// <summary>
        /// Update event
        /// </summary>
        /// <param name="name">Event Name</param>
        public static void update(string name)
        {
            if (!UpdateSession.IsConnected)
            {
                UpdateSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
                UpdateSession.LockingOption = LockingOption.None;
                UpdateSession.Connect();
            }
            XpoSQLTriggerInfo trigger;
       
                trigger = UpdateSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
         
            if (trigger != null)
            {
            
                    trigger.timestamp = (DateTime)UpdateSession.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);                
                    trigger.Save();
               
            }
        }

        /// <summary>
        /// Register an event to watch for
        /// </summary>
        /// <param name="name">Event Name</param>
        public void registerSQLTrigger(string name)
        {
            if (!UpdateSession.IsConnected)
            {
                UpdateSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
                UpdateSession.LockingOption = LockingOption.None;
                UpdateSession.Connect();
            }
            XpoSQLTriggerInfo trigger;
        
                trigger = UpdateSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
          
            if (trigger != null)
            {
                SQLTriggerEvent obj = new SQLTriggerEvent();
                obj.name = trigger.triggerName;
                obj.timestamp = trigger.timestamp;
                triggerList.Add(obj);
            }
        }

        private DateTime? getSQLTriggerTime(string name)
        {
            if (!TimeSession.IsConnected)
            {
                TimeSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
                TimeSession.LockingOption = LockingOption.None;
                TimeSession.Connect();
            }

                return TimeSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'")).timestamp;
        
        }
    }
}
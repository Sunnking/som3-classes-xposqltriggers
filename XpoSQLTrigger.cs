using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Xpo;
using DevExpress.Data.Filtering;
using System.Timers;
using System.Threading;

namespace SOM4.Classes.XpoSqlTriggers
{
    class XpoSQLTriggerInfo : XPObject
    {
        public XpoSQLTriggerInfo(Session session) : base(session) { }
        public XpoSQLTriggerInfo() : base() { }

        private string _triggerName;
        public string triggerName         
        {
            get { return _triggerName; }
            set { SetPropertyValue("triggerName", ref _triggerName, value); }
        }

        private DateTime? _timestamp;
        public DateTime? timestamp
        {
            get { return _timestamp; }
            set { SetPropertyValue("timestamp", ref _timestamp, value); }
        }
    }

    /// <summary>
    /// Database update trigger
    /// </summary>
    public class SQLTriggerEvent : EventArgs
    {
        public string name { get; set; }
        public DateTime? timestamp { get; set; }
    }

    public class XpoTrigger :IDisposable{

        public delegate void SQLTriggerHandler(Object sender, SQLTriggerEvent e);
        public event SQLTriggerHandler sqlTriggerEvent;        
        static Session UpdateSession = new Session();        
        static Session TimeSession = new Session();

        List<SQLTriggerEvent> triggerList = new List<SQLTriggerEvent>();
        System.Timers.Timer timer = new System.Timers.Timer();
        private SynchronizationContext context;        
    

        /// <summary>
        /// Create a new instance of the XpoTrigger Class
        /// </summary>
        /// <param name="time">Time in ms to refresh to look for changes (Default is 1000ms)</param>
        public XpoTrigger(double time = 1000)
        {
            UpdateSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
            UpdateSession.LockingOption = LockingOption.None;
            UpdateSession.IdentityMapBehavior = IdentityMapBehavior.Strong;


            TimeSession.OptimisticLockingReadBehavior = OptimisticLockingReadBehavior.Ignore;
            TimeSession.LockingOption = LockingOption.None;
            TimeSession.IdentityMapBehavior = IdentityMapBehavior.Strong;

            context = SynchronizationContext.Current;
            if (context == null)
            {
                context = new SynchronizationContext();
            }
            timer.AutoReset = true;
            timer.Interval = time;
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Enabled = true;                        
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
                    trigger.timestamp = (DateTime)UpdateSession.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);
                    trigger.Save();

                }
                else
                    trigger.Reload();

        }

        /// <summary>
        /// Update event
        /// </summary>
        /// <param name="name">Event Name</param>
        [Obsolete("Method is deprecated, please provide the session in the method")]
        public static void update(string name)
        {
                UpdateSession.Connect();                
                XpoSQLTriggerInfo trigger;
                trigger = UpdateSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
                if (trigger != null)
                {
                    trigger.timestamp = (DateTime)UpdateSession.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);
                    trigger.Save();
                }
                UpdateSession.Disconnect();            
        }

        /// <summary>
        /// Update event
        /// </summary>
        /// <param name="name">Event Name</param>
        public static void update(string name, Session session1)
        {            
            XpoSQLTriggerInfo trigger;
            try
            {
                trigger = session1.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));

                session1.BeginTransaction();
                if (trigger != null)
                {
                    trigger.timestamp = (DateTime)session1.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);
                    trigger.Save();
                }
                session1.CommitTransaction();
            }
            catch { }
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
            XpoSQLTriggerInfo trig;
            DateTime? returnval= null;
            lock (TimeSession)
            {
                TimeSession.Connect();
                 trig = TimeSession.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
                 if (trig != null)
                 {
                     trig.Reload();
                     returnval = trig.timestamp;
                 }
                TimeSession.Disconnect();                
            }
            return returnval;
        }

        public void Dispose()
        {
            timer.Enabled = false;
            UpdateSession.Disconnect();
            TimeSession.Disconnect();            
            triggerList.Clear();
        }
    }
}
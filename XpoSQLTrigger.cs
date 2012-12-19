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


    public class SQLTriggerEvent : EventArgs
    {
        public string name { get; set; }
        public DateTime? timestamp { get; set; }
    }

    public class XpoTrigger{

        public delegate void SQLTriggerHandler(Object sender, SQLTriggerEvent e);
        public event SQLTriggerHandler sqlTriggerEvent;        
        static Session session = new Session();
        List<SQLTriggerEvent> triggerList = new List<SQLTriggerEvent>();
        System.Timers.Timer timer = new System.Timers.Timer();
        private SynchronizationContext context;
        
        public static Object syncLock = new Object();

        public XpoTrigger(double time = 1000)
        {
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


        public static void register(string name)
        {
            if(!session.IsConnected)
                session.Connect();
            XpoSQLTriggerInfo trigger;
            lock (syncLock)
            {
                trigger = session.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
            }
            if (trigger == null)
            {
                trigger = new XpoSQLTriggerInfo(session);
                trigger.triggerName = name;
                lock (syncLock)
                {
                    trigger.timestamp = (DateTime)session.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);            
                    trigger.Save();
                }
            }

        }

        public static void update(string name)
        {
            if (!session.IsConnected)
                session.Connect();

            XpoSQLTriggerInfo trigger;
            lock (syncLock)
            {
                trigger = session.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
            }
            if (trigger != null)
            {
                lock (syncLock)
                {
                    trigger.timestamp = (DateTime)session.Evaluate(typeof(XPObjectType), new FunctionOperator(FunctionOperatorType.Now), null);                
                    trigger.Save();
                }       
            }
        }

        public void registerSQLTrigger(string name)
        {
            if (!session.IsConnected)
                session.Connect();
            XpoSQLTriggerInfo trigger;
            lock (syncLock)
            {
                trigger = session.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'"));
            }
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
            if (!session.IsConnected)
                session.Connect();
            lock (syncLock)
            {
                return session.FindObject<XpoSQLTriggerInfo>(CriteriaOperator.Parse("[triggerName] = '" + name + "'")).timestamp;
            }
        }
    }
}

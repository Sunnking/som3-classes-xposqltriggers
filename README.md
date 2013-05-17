#Sunnking Operations Manager 3.2#
## XPO SQL Trigger Class

This class allows XPO to be used in Multi-User applications and create triggers on when changes to the database have been made.

###Usage###

:::c#
using SOM3.Classes.XpoSqlTriggers;

//Register Trigger
XpoTrigger.register("Trigger Name");

//Tell Class there has been an update
XpoTrigger.update("Trigger Name");

//Do something with this info
XpoTrigger trigger = new XPOTrigger();
trigger.registerSQLTrigger("Trigger Name");
trigger.sqlTriggerEvent += TriggerEvent;

private void TriggerEvent(Object sender, SQLTriggerEvent e)
{
  // Refresh XPCollection Here
}

###Sample Program###

:::c#
XpoTrigger.register("test");
XpoTrigger.register("test2");
XpoTrigger trig = new XpoTrigger();
trig.registerSQLTrigger("test");
trig.registerSQLTrigger("test2");
trig.sqlTriggerEvent += triggerevent;

XpoTrigger.update("test");
Console.ReadLine();
XpoTrigger.update("test2");
Console.ReadLine();

private void triggerevent(Object sender, SQLTriggerEvent e)
{
	Console.WriteLine(e.name);
}

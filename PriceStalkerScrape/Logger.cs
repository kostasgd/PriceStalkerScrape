using System;
internal class Logger
{
    private readonly log4net.ILog mLog;
    private static Logger mInstance;

    private Logger()
    {
        
        //log4net.Config.XmlConfigurator.Configure();
        mLog = log4net.LogManager.GetLogger("MainForm");
    }
    internal static Logger Instance
    {
        get
        {
            if (mInstance == null)
                log4net.Config.XmlConfigurator.Configure();
                mInstance = new Logger();
            return mInstance;
        }
    }
    internal void WriteError(string message, Exception ex)
    {
        if (ex == null)
            mLog.Error(message);
        else
            mLog.Error(message, ex);
    }
    internal void WriteInfo(string message)
    {
        mLog.Info(message);
    }
    internal void WriteDebug(string message)
    {
        mLog.Debug(message);
    }
}

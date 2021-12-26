namespace bitcoin_data.Model
{
    internal abstract class BasePaymentType
    {
        public abstract ScriptType ScriptType { get; }

        public abstract string GetAddress();
    }
}

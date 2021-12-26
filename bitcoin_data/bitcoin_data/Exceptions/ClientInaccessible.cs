﻿namespace bitcoin_data.Exceptions
{
    internal class ClientInaccessible : Exception
    {
        public ClientInaccessible() : 
            base("Cannot query the Bitcoin client; " +
                "make sure the client is running " +
                "and listening on the provided " +
                "endpoint.")
        { }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Mt.GraphQL.Api
{
    public class Client
    {
        protected Query<T> CreateQuery<T>()
            where T : class => 
            new ClientQuery<T>(this);
    }
}

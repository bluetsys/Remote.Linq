﻿// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

namespace Server
{
    using Common.ServiceContracts;
    using static CommonHelper;

    internal static class Program
    {
        private static void Main()
        {
            Title("Dynamic Object with known types Server");
            using var serviceHost = WcfHelper.CreateServiceHost<QueryService>()
                .AddNetNamedPipeEndpoint<IQueryService>("net.pipe://localhost/8080/query")
                .OpenService();

            PrintSetup("The query service is ready.");
            WaitForEnterKey();
        }
    }
}

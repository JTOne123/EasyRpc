﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyRpc.AspNetCore.Authorization
{
    public class UserIsAuthorized : IEndPointMethodAuthorization
    {
        private static Task<bool> _trueTask = Task.FromResult(true);
        private static Task<bool> _falseTask = Task.FromResult(false);

        public Task<bool> AsyncAuthorize(RequestExecutionContext context)
        {
            return context.HttpContext.User?.Identity?.IsAuthenticated ?? false ? _trueTask : _falseTask;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EasyRpc.AspNetCore.Messages;

namespace EasyRpc.AspNetCore.DataAnnotations.Impl
{
    public class InstanceDataAnnotationFilter : ICallExecuteFilter
    {
        private readonly int _index;

        public InstanceDataAnnotationFilter(int index)
        {
            _index = index;
        }

        public void BeforeExecute(ICallExecutionContext context)
        {
            var instance = context.Parameters[_index];
            var validationContext = new ValidationContext(instance, context.HttpContext.RequestServices, null);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(instance, validationContext, validationResults, true))
            {
                var errorMessage = validationResults.Aggregate("", (current, result) =>
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        current += result.ErrorMessage + Environment.NewLine;
                    }

                    return current;
                });

                if (context.ResponseMessage is ErrorResponseMessage currentResponse)
                {
                    errorMessage += currentResponse.Error;
                }

                context.ContinueCall = false;
                context.ResponseMessage = new ErrorResponseMessage( JsonRpcErrorCode.InvalidRequest, errorMessage, context.RequestMessage.Version, context.RequestMessage.Id);
            }
        }

        public void AfterExecute(ICallExecutionContext context)
        {

        }
    }
}

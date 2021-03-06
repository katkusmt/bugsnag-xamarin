﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bugsnag.Data;

namespace Bugsnag
{
    internal class ExceptionConverter
    {
        private readonly BugsnagClient client;

        public ExceptionConverter (BugsnagClient client)
        {
            if (client == null)
                throw new ArgumentNullException ("client");

            this.client = client;
        }

        public List<ExceptionInfo> Convert (Exception ex)
        {
            var list = new List<ExceptionInfo> ();

            while (ex != null) {
                ExceptionInfo exInfo;
                if (ex is Java.Lang.Throwable) {
                    exInfo = ConvertThrowable ((Java.Lang.Throwable)ex);
                } else {
                    exInfo = ConvertException (ex);
                }

                list.Add (exInfo);
                ex = ex.InnerException;
            }

            return list;
        }

        private ExceptionInfo ConvertException (Exception ex)
        {
            var type = ex.GetType ();
            var trace = new StackTrace (ex, true);

            return new ExceptionInfo () {
                Name = type.Name,
                Message = ex.Message,
                Stack = trace.GetFrames ().Select ((frame) => {
                    var method = frame.GetMethod ();
                    return new StackInfo () {
                        Method = String.Format ("{0}:{1}", method.DeclaringType.FullName, method.Name),
                        File = frame.GetFileName () ?? "Unknown",
                        Line = frame.GetFileLineNumber (),
                        Column = frame.GetFileColumnNumber (),
                        InProject = IsInProject (method.DeclaringType.FullName),
                    };
                }).ToList (),
            };
        }

        private ExceptionInfo ConvertThrowable (Java.Lang.Throwable ex)
        {
            var type = ex.GetType ();

            return new ExceptionInfo () {
                Name = type.Name,
                Message = ex.LocalizedMessage,
                Stack = ex.GetStackTrace ().Select ((frame) => new StackInfo () {
                    Method = String.Format ("{0}:{1}", frame.ClassName, frame.MethodName),
                    File = frame.FileName ?? "Unknown",
                    Line = frame.LineNumber,
                    InProject = IsInProject (frame.ClassName),
                }).ToList (),
            };
        }

        private bool IsInProject (string fullName)
        {
            var namespaces = client.ProjectNamespaces;
            if (namespaces == null)
                return false;

            return namespaces.Any (fullName.StartsWith);
        }
    }
}

﻿using Sigil.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sigil
{
    /// <summary>
    /// A SigilVerificationException is thrown whenever a CIL stream becomes invalid.
    /// 
    /// There are many possible causes of this including: operator type mismatches, underflowing the stack, and branching from one stack state to another.
    /// 
    /// Invalid arguments, non-sensical parameters, and other non-correctness related errors will throw more specific exceptions.
    /// 
    /// SigilVerificationException will typically include the state of the stack (or stacks) at the instruction in error.
    /// </summary>
    [Serializable]
    public class SigilVerificationException : Exception, ISerializable
    {
        private string[] Instructions;
        private VerificationResult Failure;

        internal SigilVerificationException(string method, VerificationResult failure, string[] instructions)
            : this(GetMessage(method, failure), instructions)
        {
            Failure = failure;
        }

        internal SigilVerificationException(string message, string[] instructions) : base(message)
        {
            Instructions = instructions;
        }

        private static string GetMessage(string method, VerificationResult failure)
        {
            if (failure.Success) throw new Exception("What?!");

            if (failure.IsStackUnderflow)
            {
                if (failure.ExpectedStackSize == 1)
                {
                    return method + " expects a value on the stack, but it was empty";
                }

                return method + " expects " + failure.ExpectedStackSize + " values on the stack";
            }

            if (failure.IsTypeMismatch)
            {
                var expected = failure.ExpectedAtStackIndex.ErrorMessageString();
                var found = failure.Stack.ElementAt(failure.StackIndex).ErrorMessageString();

                return method + " expected " + (expected.StartsWithVowel() ? "an " : "a ") + expected + "; found " + found;
            }

            if (failure.IsStackMismatch)
            {
                return method + " resulted in stack mismatches";
            }

            if (failure.IsStackSizeFailure)
            {
                if (failure.ExpectedStackSize == 0)
                {
                    return method + " expected the stack of be empty";
                }

                if (failure.ExpectedStackSize == 1)
                {
                    return method + "expected the stack to have 1 value";
                }

                return method + " expected the stack to have " + failure.ExpectedStackSize + " values";
            }

            throw new Exception("Shouldn't be possible!");
        }

        /// <summary>
        /// Returns a string representation of any stacks attached to this exception.
        /// 
        /// This is meant for debugging purposes, and should not be called during normal operation.
        /// </summary>
        public string GetDebugInfo()
        {
            var ret = new StringBuilder();

            if (Failure.IsStackMismatch)
            {
                ret.AppendLine("Expected Stack");
                ret.AppendLine("==============");
                PrintStack(Failure.ExpectedStack, ret);

                ret.AppendLine();
                ret.AppendLine("Incoming Stack");
                ret.AppendLine("==============");
                PrintStack(Failure.IncomingStack, ret);
            }

            if (Failure.IsTypeMismatch)
            {
                ret.AppendLine("Stack");
                ret.AppendLine("=====");
                PrintStack(Failure.Stack, ret, "// bad value", Failure.StackIndex);
            }

            if (Failure.IsStackUnderflow || Failure.IsStackSizeFailure)
            {
                ret.AppendLine("Stack");
                ret.AppendLine("=====");
                PrintStack(Failure.Stack, ret);
            }

            ret.AppendLine();
            ret.AppendLine("Instructions");
            ret.AppendLine("============");

            var instrIx = Failure.Verifier.GetInstructionIndex(Failure.TransitionIndex);

            for(var i = 2; i < Instructions.Length; i++)
            {
                var line = Instructions[i];

                if (i == instrIx) line = line + "  // relevant instruction";

                if (!string.IsNullOrEmpty(line))
                {
                    ret.AppendLine(line);
                }
            }

            return ret.ToString();
        }

        private static void PrintStack(Stack<IEnumerable<TypeOnStack>> stack, StringBuilder sb, string mark = null, int? markAt = null)
        {
            if (stack.Count == 0)
            {
                sb.AppendLine("--empty--");
                return;
            }

            for (var i = 0; i < stack.Count; i++)
            {
                var asStr =
                    string.Join(", or",
                        stack.ElementAt(i).Select(s => s.ToString()).ToArray()
                    );

                if (i == markAt)
                {
                    asStr += "  " + mark;
                }

                sb.AppendLine(asStr);
            }
        }

        /// <summary>
        /// Implementation for ISerializable.
        /// </summary>
#if !NET35
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new System.ArgumentNullException("info");
            }

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Returns the message and stacks on this exception, in string form.
        /// </summary>
#if !NET35
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
        public override string ToString()
        {
            return
                Message + Environment.NewLine + Environment.NewLine + GetDebugInfo();
        }
    }
}

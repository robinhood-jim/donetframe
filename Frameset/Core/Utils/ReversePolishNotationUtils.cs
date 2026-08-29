using Microsoft.IdentityModel.Tokens;
using Serilog;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Frameset.Core.Utils
{
    public static class ReversePolishNotationUtils
    {
        private static Dictionary<char, int> operSymbolDict = [];
        private static char LEFTCHAR = '(';
        private static char RIGHTCHAR = ')';
        private static ThreadLocal<Queue<string>> calculateQueue;
        static ReversePolishNotationUtils()
        {
            operSymbolDict.TryAdd(')', 0);
            operSymbolDict.TryAdd('(', 30);
            operSymbolDict.TryAdd('+', 10);
            operSymbolDict.TryAdd('-', 10);
            operSymbolDict.TryAdd('*', 20);
            operSymbolDict.TryAdd('/', 20);
        }
        public static Queue<string> Parse(string formula)
        {
            Stack<string> preStack = [];
            Queue<string> queue = [];
            StringBuilder builder = new();
            char[] chars = formula.ToArray();
            int i = 0;
            while (i < chars.Length)
            {
                if (operSymbolDict.TryGetValue(chars[i], out _))
                {
                    if (builder.Length > 0)
                    {
                        queue.Enqueue(builder.ToString());
                        builder.Clear();
                    }
                    if (preStack.IsNullOrEmpty())
                    {
                        preStack.Push(chars[i].ToString());
                    }
                    else
                    {
                        string top = preStack.Pop();
                        if (!comparePriority(chars[i], top[0]))
                        {
                            if (top[0].Equals(LEFTCHAR))
                            {
                                preStack.Push(top);
                                preStack.Push(chars[i].ToString());
                            }
                            else if (chars[i].Equals(RIGHTCHAR))
                            {
                                AppendTo(queue, top);
                                preStack.Pop();
                            }
                            else
                            {
                                AppendTo(queue, top);
                                PopPre(preStack, chars[i], queue);
                                preStack.Push(chars[i].ToString());
                            }
                        }
                        else
                        {
                            preStack.Push(top);
                            preStack.Push(chars[i].ToString());
                        }
                    }
                }
                else
                {
                    builder.Append(chars[i]);
                }
                i++;
            }
            if (builder.Length > 0)
            {
                queue.Enqueue(builder.ToString().Trim());
            }
            while (!preStack.IsNullOrEmpty())
            {
                queue.Enqueue(preStack.Pop().Trim());
            }
            return queue;
        }
        public static double? Compute(Queue<string> queue, Dictionary<string, object> valueDict)
        {
            double result = 0.0;
            if (queue.IsNullOrEmpty())
            {
                return null;
            }
            if (calculateQueue == null || calculateQueue.IsValueCreated)
            {
                calculateQueue = new ThreadLocal<Queue<string>>(() => []);

            }
            calculateQueue.Value.Clear();
            string value;
            while (queue.Count > 0 && (value = queue.Dequeue()) != null)
            {
                calculateQueue.Value.Enqueue(value);
            }
            string element = calculateQueue.Value.Dequeue();
            Stack<double> stack = [];
            try
            {
                while (!string.IsNullOrWhiteSpace(element))
                {
                    if (!operSymbolDict.TryGetValue(element[0], out _))
                    {
                        if (valueDict.TryGetValue(element, out object selVal))
                        {
                            stack.Push(Convert.ToDouble(selVal));
                        }
                        else
                        {
                            if (NumberUtils.IsNumber(element))
                            {
                                stack.Push(Convert.ToDouble(element));
                            }
                            else
                            {
                                stack.Push(0.0);
                            }
                        }
                    }
                    else
                    {
                        switch (element[0])
                        {
                            case '+':
                                result = stack.Pop() + stack.Pop();
                                break;
                            case '-':
                                double leftValue = stack.Pop();
                                double rightValue = stack.Pop();
                                result = rightValue - leftValue;
                                break;
                            case '*':
                                result = stack.Pop() * stack.Pop();
                                break;
                            case '/':
                                double leftValue1 = stack.Pop();
                                double rightValue1 = stack.Pop();
                                Trace.Assert(leftValue1 != 0.0, "divided by zero");
                                result = rightValue1 / leftValue1;
                                break;
                        }
                        stack.Push(result);
                    }

                    if (calculateQueue.Value.Count > 0)
                    {
                        element = calculateQueue.Value.Dequeue();
                    }
                    else
                    {
                        element = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return result;
        }
        private static bool comparePriority(char start, char to)
        {
            if (operSymbolDict.TryGetValue(start, out int startP) && operSymbolDict.TryGetValue(to, out int toP))
            {
                return startP > toP;
            }
            return false;
        }
        private static void AppendTo(Queue<string> queue, string s)
        {
            if (!s[0].Equals(LEFTCHAR) && !s[0].Equals(RIGHTCHAR))
            {
                queue.Enqueue(s.ToString().Trim());
            }
        }
        private static void PopPre(Stack<string> preStack, char charTemp, Queue<string> queue)
        {
            if (!preStack.IsNullOrEmpty())
            {
                string top = preStack.Pop();
                if (!comparePriority(charTemp, top[0]))
                {
                    AppendTo(queue, top);
                    PopPre(preStack, charTemp, queue);
                }
                else
                {
                    preStack.Push(top);
                }
            }
        }
    }
}

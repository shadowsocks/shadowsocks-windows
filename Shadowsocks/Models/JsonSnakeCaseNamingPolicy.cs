// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Source: https://github.com/dotnet/corefx/pull/40003
// See also: https://github.com/dotnet/runtime/issues/782

using System;
using System.Text;
using System.Text.Json;

namespace Shadowsocks.Models
{
    public class JsonSnakeCaseNamingPolicy : JsonNamingPolicy
    {
        internal enum SnakeCaseState
        {
            Start,
            Lower,
            Upper,
            NewWord
        }

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var sb = new StringBuilder();
            var state = SnakeCaseState.Start;

            var nameSpan = name.AsSpan();

            for (int i = 0; i < nameSpan.Length; i++)
            {
                if (nameSpan[i] == ' ')
                {
                    if (state != SnakeCaseState.Start)
                    {
                        state = SnakeCaseState.NewWord;
                    }
                }
                else if (char.IsUpper(nameSpan[i]))
                {
                    switch (state)
                    {
                        case SnakeCaseState.Upper:
                            bool hasNext = (i + 1 < nameSpan.Length);
                            if (i > 0 && hasNext)
                            {
                                char nextChar = nameSpan[i + 1];
                                if (!char.IsUpper(nextChar) && nextChar != '_')
                                {
                                    sb.Append('_');
                                }
                            }
                            break;
                        case SnakeCaseState.Lower:
                        case SnakeCaseState.NewWord:
                            sb.Append('_');
                            break;
                    }
                    sb.Append(char.ToLowerInvariant(nameSpan[i]));
                    state = SnakeCaseState.Upper;
                }
                else if (nameSpan[i] == '_')
                {
                    sb.Append('_');
                    state = SnakeCaseState.Start;
                }
                else
                {
                    if (state == SnakeCaseState.NewWord)
                    {
                        sb.Append('_');
                    }

                    sb.Append(nameSpan[i]);
                    state = SnakeCaseState.Lower;
                }
            }

            return sb.ToString();
        }
    }
}

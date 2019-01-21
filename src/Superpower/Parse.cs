﻿// Copyright 2016 Datalust, Superpower Contributors, Sprache Contributors
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at  
//
//     http://www.apache.org/licenses/LICENSE-2.0  
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Superpower.Model;
using Superpower.Util;
using System;
using Superpower.Display;

namespace Superpower
{
    /// <summary>
    /// General parsing helper methods.
    /// </summary>
    public static class Parse
    {
        /// <summary>
        /// Parse a sequence of operands connected by left-associative operators.
        /// </summary>
        /// <typeparam name="T">The type being parsed.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining an operator and two operands into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on pairs of operands.</returns>
        public static TextParser<T> Chain<T, TOperator>(
            TextParser<TOperator> @operator,
            TextParser<T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            return input =>
            {
                var parseResult = operand(input);
                if (!parseResult.HasValue)
                    return parseResult;

                var result = parseResult.Value;

                var operatorResult = @operator(parseResult.Remainder);
                while (operatorResult.HasValue || operatorResult.IsPartial(parseResult.Remainder))
                {
                    // If operator read any input, but failed to read complete input, we return error
                    if (!operatorResult.HasValue)
                        return Result.CastEmpty<TOperator,T>(operatorResult);

                    parseResult = operand(operatorResult.Remainder);

                    if (!parseResult.HasValue)
                        return parseResult;

                    result = apply(operatorResult.Value, result, parseResult.Value);
                    operatorResult = @operator(parseResult.Remainder);
                }

                return Result.Value(result, input, parseResult.Remainder);
            };
        }

        /// <summary>
        /// Parse a sequence of operands connected by right-associative operators.
        /// </summary>
        /// <typeparam name="T">The type being parsed.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining an operator and two operands into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on pairs of operands.</returns>
        public static TextParser<T> ChainRight<T, TOperator>(
            TextParser<TOperator> @operator,
            TextParser<T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return operand.Then(first => ChainRightOperatorRest(first, @operator, operand, apply));
        }

        static TextParser<T> ChainRightOperatorRest<T, TOperator>(
            T lastOperand,
            TextParser<TOperator> @operator,
            TextParser<T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return @operator.Then(opvalue =>
                operand.Then(operandValue =>
                    ChainRightOperatorRest(operandValue, @operator, operand, apply)).Then(r => Return(apply(opvalue, lastOperand, r))))
                    .Or(Return(lastOperand));
        }

        /// <summary>
        /// Parse a sequence of operands connected by left-associative operators.
        /// </summary>
        /// <typeparam name="T">The type being parsed.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining an operator and two operands into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on pairs of operands.</returns>
        public static TokenListParser<TKind, T> Chain<TKind, T, TOperator>(
            TokenListParser<TKind, TOperator> @operator,
            TokenListParser<TKind, T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            return input =>
            {
                var parseResult = operand(input);
                if ( !parseResult.HasValue )
                    return parseResult;

                var result = parseResult.Value;

                var operatorResult = @operator(parseResult.Remainder);
                while (operatorResult.HasValue || operatorResult.IsPartial(parseResult.Remainder))
                {
                    // If operator read any input, but failed to read complete input, we return error
                    if (!operatorResult.HasValue) 
                        return TokenListParserResult.CastEmpty<TKind, TOperator, T>(operatorResult);

                    parseResult = operand(operatorResult.Remainder);

                    if (!parseResult.HasValue)
                        return TokenListParserResult.CastEmpty<TKind, T, T>(parseResult);

                    result = apply(operatorResult.Value, result, parseResult.Value);
                    operatorResult = @operator(parseResult.Remainder);
                }

                return TokenListParserResult.Value(result, input, parseResult.Remainder);
            };
        }

        /// <summary>
        /// Parse a sequence of operands connected by right-associative operators.
        /// </summary>
        /// <typeparam name="T">The type being parsed.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining an operator and two operands into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on pairs of operands.</returns>
        public static TokenListParser<TKind, T> ChainRight<TKind, T, TOperator>(
            TokenListParser<TKind, TOperator> @operator,
            TokenListParser<TKind, T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return operand.Then(first => ChainRightOperatorRest(first, @operator, operand, apply));
        }

        static TokenListParser<TKind, T> ChainRightOperatorRest<TKind, T, TOperator>(
            T lastOperand,
            TokenListParser<TKind, TOperator> @operator,
            TokenListParser<TKind, T> operand,
            Func<TOperator, T, T, T> apply)
        {
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            return @operator.Then(opvalue =>
                operand.Then(operandValue =>
                    ChainRightOperatorRest(operandValue, @operator, operand, apply)).Then(r => Return<TKind, T>(apply(opvalue, lastOperand, r))))
                    .Or(Return<TKind, T>(lastOperand));
        }

        /// <summary>
        /// Parse a sequence of operands connected by a left-associative operator
        /// starting with a seed parser.
        /// </summary>
        /// <typeparam name="TResult">The type of the seed parser and the result.</typeparam>
        /// <typeparam name="TOperand">The type of the operands.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <param name="seed">The initial parser that starts the sequence.</param>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining the intermediate result with an operator and an operand into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on intermediate results and operands.</returns>
        public static TextParser<TResult> ChainLeft<TOperand, TResult, TOperator>(
            this TextParser<TResult> seed,
            TextParser<TOperator> @operator,
            TextParser<TOperand> operand,
            Func<TResult, TOperator, TOperand, TResult> apply)
        {
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            return input =>
            {
                var seedResult = seed(input);
                if (!seedResult.HasValue)
                    return seedResult;

                var result = seedResult.Value;

                var operatorResult = @operator(seedResult.Remainder);
                while (operatorResult.HasValue)
                {
                    // If operator read any input, but failed to read complete input, we return error
                    if (!operatorResult.HasValue)
                        return Result.CastEmpty<TOperator, TResult>(operatorResult);

                    var operandResult = operand(operatorResult.Remainder);

                    if (!operandResult.HasValue)
                        return Result.CastEmpty<TOperand, TResult>(operandResult);

                    result = apply(result, operatorResult.Value, operandResult.Value);
                    operatorResult = @operator(operandResult.Remainder);
                }

                return Result.Value(result, input, operatorResult.Remainder);
            };
        }

        /// <summary>
        /// Parse a sequence of operands connected by a left-associative operator
        /// starting with a seed parser.
        /// </summary>
        /// <typeparam name="TResult">The type of the seed parser and the result.</typeparam>
        /// <typeparam name="TOperand">The type of the operands.</typeparam>
        /// <typeparam name="TOperator">The type of the operator.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <param name="seed">The initial parser that starts the sequence.</param>
        /// <param name="operator">A parser matching operators.</param>
        /// <param name="operand">A parser matching operands.</param>
        /// <param name="apply">A function combining the intermediate result with an operator and an operand into the result.</param>
        /// <returns>The result of calling <paramref name="apply"/> successively on intermediate results and operands.</returns>
        public static TokenListParser<TKind, TResult> ChainLeft<TKind, TOperand, TResult, TOperator>(
            this TokenListParser<TKind, TResult> seed,
            TokenListParser<TKind, TOperator> @operator,
            TokenListParser<TKind, TOperand> operand,
            Func<TResult, TOperator, TOperand, TResult> apply)
        {
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (@operator == null) throw new ArgumentNullException(nameof(@operator));
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            if (apply == null) throw new ArgumentNullException(nameof(apply));

            return input =>
            {
                var seedResult = seed(input);
                if (!seedResult.HasValue)
                    return seedResult;

                var result = seedResult.Value;

                var operatorResult = @operator(seedResult.Remainder);
                while (operatorResult.HasValue)
                {
                    // If operator read any input, but failed to read complete input, we return error
                    if (!operatorResult.HasValue)
                        return TokenListParserResult.CastEmpty<TKind, TOperator, TResult>(operatorResult);

                    var operandResult = operand(operatorResult.Remainder);

                    if (!operandResult.HasValue)
                        return TokenListParserResult.CastEmpty<TKind, TOperand, TResult>(operandResult);

                    result = apply(result, operatorResult.Value, operandResult.Value);
                    operatorResult = @operator(operandResult.Remainder);
                }

                return TokenListParserResult.Value(result, input, operatorResult.Remainder);
            };
        }
        
        /// <summary>
        /// Constructs a parser that will fail if the given parser succeeds,
        /// and will succeed if the given parser fails. In any case, it won't
        /// consume any input. It's like a negative look-ahead in a regular expression.
        /// </summary>
        /// <typeparam name="T">The result type of the given parser</typeparam>
        /// <param name="parser">The parser to wrap</param>
        /// <returns>A parser that is the negation of the given parser.</returns>
        public static TextParser<Unit> Not<T>(TextParser<T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return input =>
            {
                var result = parser(input);

                if (result.HasValue)
                    return Result.Empty<Unit>(input, $"unexpected successful parsing of `{input.Until(result.Remainder)}`");

                return Result.Value(Unit.Value, input, input);
            };
        }

        /// <summary>
        /// Constructs a parser that will fail if the given parser succeeds,
        /// and will succeed if the given parser fails. In any case, it won't
        /// consume any input. It's like a negative look-ahead in a regular expression.
        /// </summary>
        /// <typeparam name="T">The result type of the given parser.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <param name="parser">The parser to wrap</param>
        /// <returns>A parser that is the negation of the given parser.</returns>
        public static TokenListParser<TKind, Unit> Not<TKind, T>(TokenListParser<TKind, T> parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            return input =>
            {
                var result = parser(input);

                if (result.HasValue)
                {
                    // This is usually a success case for Not(), so the allocations here are a bit of a pity.

                    var current = input.ConsumeToken();
                    var last = result.Remainder.ConsumeToken();
                    if (current.HasValue)
                    {
                        var span = last.HasValue ?
                            current.Value.Span.Source.Substring(current.Value.Position.Absolute, last.Value.Position.Absolute - current.Value.Position.Absolute) :
                            current.Value.Span.Source.Substring(current.Value.Position.Absolute);
                        return TokenListParserResult.Empty<TKind, Unit>(input, $"unexpected successful parsing of {Presentation.FormatLiteral(Friendly.Clip(span, 12))}");
                    }

                    return TokenListParserResult.Empty<TKind, Unit>(input, "unexpected successful parsing");
                }

                return TokenListParserResult.Value(Unit.Value, input, input);
            };
        }

        /// <summary>
        /// Lazily construct a parser, so that circular dependencies are possible.
        /// </summary>
        /// <param name="reference">A function creating the parser, when required.</param>
        /// <typeparam name="T">The type of value being parsed.</typeparam>
        /// <returns>A parser that lazily evaluates <paramref name="reference"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reference"/> is null.</exception>
        public static TextParser<T> Ref<T>(Func<TextParser<T>> reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            TextParser<T> parser = null;

            return i =>
            {
                if (parser == null)
                    parser = reference();

                return parser(i);
            };
        }

        /// <summary>
        /// Lazily construct a parser, so that circular dependencies are possible.
        /// </summary>
        /// <param name="reference">A function creating the parser, when required.</param>
        /// <typeparam name="T">The type of value being parsed.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <returns>A parser that lazily evaluates <paramref name="reference"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reference"/> is null.</exception>
        public static TokenListParser<TKind, T> Ref<TKind, T>(Func<TokenListParser<TKind, T>> reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));

            TokenListParser<TKind, T> parser = null;

            return i =>
            {
                if (parser == null)
                    parser = reference();

                return parser(i);
            };
        }

        /// <summary>
        /// Construct a parser with a fixed value.
        /// </summary>
        /// <param name="value">The value returned by the parser.</param>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <returns>The parser.</returns>
        public static TextParser<T> Return<T>(T value)
        {
            return input => Result.Value(value, input, input);
        }

        /// <summary>
        /// Construct a parser with a fixed value.
        /// </summary>
        /// <param name="value">The value returned by the parser.</param>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <typeparam name="TKind">The kind of token being parsed.</typeparam>
        /// <returns>The parser.</returns>
        public static TokenListParser<TKind, T> Return<TKind, T>(T value)
        {
            return input => TokenListParserResult.Value(value, input, input);
        }
    }
}

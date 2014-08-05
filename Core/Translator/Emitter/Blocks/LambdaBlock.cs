﻿using ICSharpCode.NRefactory.CSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Text;
using Mono.Cecil;
using Ext.Net.Utilities;
using ICSharpCode.NRefactory.Semantics;

namespace Bridge.NET
{
    public class LambdaBlock : AbstractMethodBlock
    {
        public LambdaBlock(Emitter emitter, LambdaExpression lambdaExpression) 
            : this(emitter, lambdaExpression.Parameters, lambdaExpression.Body, lambdaExpression, lambdaExpression.IsAsync)
        {            
        }

        public LambdaBlock(Emitter emitter, AnonymousMethodExpression anonymousMethodExpression)
            : this(emitter, anonymousMethodExpression.Parameters, anonymousMethodExpression.Body, anonymousMethodExpression, anonymousMethodExpression.IsAsync)
        {
        }

        public LambdaBlock(Emitter emitter, IEnumerable<ParameterDeclaration> parameters, AstNode body, AstNode context, bool isAsync)
        {
            this.Emitter = emitter;
            this.Parameters = parameters;
            this.Body = body;
            this.Context = context;
            this.IsAsync = isAsync;
        }

        public bool IsAsync
        {
            get;
            set;
        }

        public IEnumerable<ParameterDeclaration> Parameters 
        { 
            get; 
            set; 
        }

        public AstNode Body 
        { 
            get; 
            set; 
        }

        public AstNode Context 
        { 
            get; 
            set; 
        }

        public override void Emit()
        {
            this.EmitLambda(this.Parameters, this.Body, this.Context);            
        }

        protected virtual void EmitLambda(IEnumerable<ParameterDeclaration> parameters, AstNode body, AstNode context)
        {
            this.PushLocals();
            this.AddLocals(parameters);

            bool block = body is BlockStatement;            
            this.Write("");

            var savedPos = this.Emitter.Output.Length;
            var savedThisCount = this.Emitter.ThisRefCounter;

            this.WriteFunction();
            this.EmitMethodParameters(parameters, context);
            this.WriteSpace();

            if (!block && !this.IsAsync)
            {
                this.WriteOpenBrace();
                this.WriteSpace();
            }

            if (body.Parent is LambdaExpression && !block && !this.IsAsync)
            {                
                this.WriteReturn(true);
            }

            if (this.IsAsync)
            {
                if (context is LambdaExpression)
                {
                    new AsyncBlock(this.Emitter, (LambdaExpression)context).Emit();
                }
                else
                {
                    new AsyncBlock(this.Emitter, (AnonymousMethodExpression)context).Emit();
                }                
            }
            else
            {
                body.AcceptVisitor(this.Emitter);
            }

            if (!block && !this.IsAsync)
            {
                this.WriteSpace();
                this.WriteCloseBrace();
            }

            if (this.Emitter.ThisRefCounter > savedThisCount)
            {
                this.Emitter.Output.Insert(savedPos, Emitter.ROOT + "." + Emitter.DELEGATE_BIND + "(this, ");
                this.WriteCloseParentheses();
            }

            this.PopLocals();
        }
    }
}
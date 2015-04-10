﻿// This software is part of the Autofac IoC container
// Copyright (c) 2012 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;

namespace Autofac.Integration.WebApi
{
    /// <summary>
    /// Resolves a filter for the specified metadata for each controller request.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "Derived attribute adds filter override support")]
    [SuppressMessage("Microsoft.Security", "CA2146:TypesMustBeAtLeastAsCriticalAsBaseTypesFxCopRule")]
    internal class ActionFilterWrapper : ActionFilterAttribute, IAutofacActionFilter, IFilterWrapper
    {
        [SuppressMessage("Microsoft.Security", "CA2151:FieldsWithCriticalTypesShouldBeCriticalFxCopRule")]
        readonly FilterMetadata _filterMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionFilterWrapper"/> class.
        /// </summary>
        /// <param name="filterMetadata">The filter metadata.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        public ActionFilterWrapper(FilterMetadata filterMetadata)
        {
            if (filterMetadata == null) throw new ArgumentNullException("filterMetadata");

            _filterMetadata = filterMetadata;
        }

        /// <summary>
        /// Gets the metadata key used to retrieve the filter metadata.
        /// </summary>
        public virtual string MetadataKey
        {
            [SecurityCritical]
            get { return AutofacWebApiFilterProvider.ActionFilterMetadataKey; }
        }

        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The context for the action.</param>
        /// <param name="cancellationToken">The cancellation token assigned for this task.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="actionContext" /> is <see langword="null" />.
        /// </exception>
        [SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        [SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule")]
        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }
            var dependencyScope = actionContext.Request.GetDependencyScope();
            var lifetimeScope = dependencyScope.GetRequestLifetimeScope();

            var filters = lifetimeScope.Resolve<IEnumerable<Meta<Lazy<IAutofacActionFilter>>>>();

            foreach (var filter in filters.Where(FilterMatchesMetadata))
                await filter.Value.Value.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        /// <summary>
        /// Occurs after the action method is invoked.
        /// </summary>
        /// <param name="actionExecutedContext">The context for the action.</param>
        /// <param name="cancellationToken">The cancellation token assigned for this task.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="actionExecutedContext" /> is <see langword="null" />.
        /// </exception>
        [SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        [SuppressMessage("Microsoft.Security", "CA2134:MethodsMustOverrideWithConsistentTransparencyFxCopRule")]
        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext == null)
            {
                throw new ArgumentNullException("actionExecutedContext");
            }
            var dependencyScope = actionExecutedContext.Request.GetDependencyScope();
            var lifetimeScope = dependencyScope.GetRequestLifetimeScope();

            var filters = lifetimeScope.Resolve<IEnumerable<Meta<Lazy<IAutofacActionFilter>>>>();

            foreach (var filter in filters.Where(FilterMatchesMetadata))
                await filter.Value.Value.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }

        [SuppressMessage("Microsoft.Security", "CA2140:TransparentMethodsMustNotReferenceCriticalCodeFxCopRule")]
        bool FilterMatchesMetadata(Meta<Lazy<IAutofacActionFilter>> filter)
        {
            var metadata = filter.Metadata.ContainsKey(MetadataKey)
                ? filter.Metadata[MetadataKey] as FilterMetadata : null;

            return metadata != null
                && metadata.ControllerType == _filterMetadata.ControllerType
                && metadata.FilterScope == _filterMetadata.FilterScope
                && metadata.MethodInfo == _filterMetadata.MethodInfo;
        }
    }
}
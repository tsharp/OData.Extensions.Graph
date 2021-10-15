using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.OData.Extensions.GraphQL
{
    public sealed class GraphEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal GraphEndpointConventionBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) =>
            _builder.Add(convention);
    }
}

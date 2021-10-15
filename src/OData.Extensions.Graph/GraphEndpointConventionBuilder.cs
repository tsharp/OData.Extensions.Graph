using Microsoft.AspNetCore.Builder;
using System;

namespace OData.Extensions.Graph
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

using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace uhttpsharp.Tests
{
    public class HttpMethodProviderTests
    {

        private static IHttpMethodProvider GetTarget()
        {
            return new HttpMethodProvider();
        }


        public static IEnumerable<object> Methods
        {
            get
            {
                return Enum.GetNames(typeof(HttpMethods));
            }
        }
        
        [Theory]
        [InlineData(HttpMethods.Connect)]
        [InlineData(HttpMethods.Delete)]
        [InlineData(HttpMethods.Get)]
        [InlineData(HttpMethods.Head)]
        [InlineData(HttpMethods.Options)]
        [InlineData(HttpMethods.Patch)]
        [InlineData(HttpMethods.Post)]
        [InlineData(HttpMethods.Put)]
        [InlineData(HttpMethods.Trace)]
        public void Should_Get_Right_Method(HttpMethods method)
        {
            // Arrange
            var methodName = Enum.GetName(typeof(HttpMethods), method);
            var target = GetTarget();

            // Act
            var actual = target.Provide(methodName);

            // Assert
            actual.ToString().ShouldBe(methodName);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using Phoenix.Functionality.Ioc.Autofac;

namespace Functionality.Ioc.Autofac.Test
{
	public class IocScopeVerificationTest
	{
#pragma warning disable 8618 // → Always initialized in the 'Setup' method before a test is run.
		private IFixture _fixture;
#pragma warning restore 8618

		[SetUp]
		public void Setup()
		{
			_fixture = new Fixture().Customize(new AutoMoqCustomization());
		}

		/// <summary> Checks that an immediately thrown <see cref="Exception"/> in a verification method is properly caught. This is different from throwing after anything has been yielded, as the enumerator is not even started. </summary>
		[Test]
		public void Check_Executing_Verification_Methods_Catches_Immediate_Exception()
		{
			// Arrange
			var targetException = new ApplicationException();
			IAsyncEnumerable<string> VerificationMethod(CancellationToken token)
			{
				throw targetException;
			}

			// Act
			var asyncEnumerable = LifetimeScopeExtensions.ExecuteVerificationMethodsAsync(new[] { (IocScopeVerificationDelegate) VerificationMethod });

			// Assert
			var actualException = Assert.CatchAsync<IocScopeVerificationException>(async () => { await foreach (var _ in asyncEnumerable) { } });
			Assert.AreSame(targetException, actualException.InnerException);
		}

		/// <summary> Checks that any <see cref="Exception"/> a verification method throws, is wrapped within a <see cref="IocScopeVerificationException"/>. </summary>
		[Test]
		public void Check_Executing_Verification_Methods_Does_Only_Throw_IocModuleVerificationException()
		{
			// Arrange
			var targetException = new ApplicationException();
			async IAsyncEnumerable<string> VerificationMethod(CancellationToken token)
			{
				await Task.Delay(100, token);
				yield return "Something";
				throw targetException;
			}

			// Act
			var asyncEnumerable = LifetimeScopeExtensions.ExecuteVerificationMethodsAsync(new[] { (IocScopeVerificationDelegate)VerificationMethod });

			// Assert
			var actualException = Assert.CatchAsync<IocScopeVerificationException>(async () => { await foreach (var _ in asyncEnumerable) { } });
			Assert.AreSame(targetException, actualException.InnerException);
		}

		/// <summary> Checks that executing verification methods can be cancelled and that an <see cref="OperationCanceledException"/> is then thrown. </summary>
		[Test]
		public void Check_Executing_Verification_Methods_Throw_On_Immediate_Cancellation()
		{
			// Arrange
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();
			IAsyncEnumerable<string> VerificationMethod(CancellationToken token)
			{
				token.ThrowIfCancellationRequested();
				throw new ApplicationException();
			}

			// Act
			var asyncEnumerable = LifetimeScopeExtensions.ExecuteVerificationMethodsAsync(new[] { (IocScopeVerificationDelegate) VerificationMethod }, cancellationTokenSource.Token);

			// Assert
			//Assert.DoesNotThrowAsync(async () => { await foreach (var _ in asyncEnumerable.WithCancellation(cancellationTokenSource.Token)) { } });
			Assert.CatchAsync<OperationCanceledException>(async () => { await foreach (var _ in asyncEnumerable.WithCancellation(cancellationTokenSource.Token)) { } });
		}

		/// <summary> Checks that executing verification methods can be cancelled and that an <see cref="OperationCanceledException"/> is then thrown. </summary>
		[Test]
		public void Check_Executing_Verification_Methods_Throw_On_Cancellation()
		{
			// Arrange
			var cancellationTokenSource = new CancellationTokenSource();
			cancellationTokenSource.Cancel();
			async IAsyncEnumerable<string> VerificationMethod(CancellationToken token)
			{
				token.ThrowIfCancellationRequested();
				await Task.Delay(100, token);
				yield return "Something";
			}

			// Act
			var asyncEnumerable = LifetimeScopeExtensions.ExecuteVerificationMethodsAsync(new[] { (IocScopeVerificationDelegate)VerificationMethod }, cancellationTokenSource.Token);

			// Assert
			//Assert.DoesNotThrowAsync(async () => { await foreach (var _ in asyncEnumerable.WithCancellation(cancellationTokenSource.Token)) { } });
			Assert.CatchAsync<OperationCanceledException>(async () => { await foreach (var _ in asyncEnumerable.WithCancellation(cancellationTokenSource.Token)) { } });
		}
	}
}
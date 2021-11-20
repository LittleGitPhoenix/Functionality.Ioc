using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Autofac;

namespace Phoenix.Functionality.Ioc.Autofac
{
	/// <summary>
	/// Delegate for verification functions used to perform checks of build <see cref="ILifetimeScope"/>s.
	/// </summary>
	/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
	/// <returns> An async enumerable of <see cref="string"/>s. </returns>
	public delegate IAsyncEnumerable<string> IocScopeVerificationDelegate(CancellationToken cancellationToken = default);

	/// <summary>
	/// Exception that should be thrown by a verification method (<see cref="IocScopeVerificationDelegate"/>).
	/// </summary>
	/// <remarks> This exception is also used as wrapper, when executing <see cref="IocScopeVerificationDelegate"/>s via the extension <see cref="ILifetimeScope.ExecuteVerificationMethodAsync"/> when the delegate throws any other kind of exception. </remarks>
	public class IocScopeVerificationException : Exception
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message"> <see cref="Exception.Message"/> </param>
		/// /// <param name="innerException"> <see cref="Exception.InnerException"/> </param>
		public IocScopeVerificationException(string message, Exception? innerException = null) : base(message, innerException) { }
	}

	/// <summary>
	/// Contains extension methods for <see cref="ILifetimeScope"/>.
	/// </summary>
	public static partial class LifetimeScopeExtensions
	{
		/// <summary>
		/// Executes verification methods in form of the <see cref="IocScopeVerificationDelegate"/> resolved from <paramref name="scope"/>.
		/// </summary>
		/// <param name="scope"> The <see cref="ILifetimeScope"/> used to resolve the verification methods. </param>
		/// <param name="cancellationToken"> Optional <see cref="CancellationToken"/>. </param>
		/// <returns> An asynchronous enumerable of messages produced by the verification methods. </returns>
		/// <exception cref="IocScopeVerificationException"> Thrown if verification failed. </exception>
		/// <exception cref="OperationCanceledException"> Is handled gracefully by simply stopping the enumeration. </exception>
		public static IAsyncEnumerable<string> ExecuteVerificationMethodsAsync(this ILifetimeScope scope, CancellationToken cancellationToken = default)
			=> LifetimeScopeExtensions.ExecuteVerificationMethodsAsync(scope.Resolve<ICollection<IocScopeVerificationDelegate>>(), cancellationToken);

		internal static async IAsyncEnumerable<string> ExecuteVerificationMethodsAsync(ICollection<IocScopeVerificationDelegate> verificationDelegates, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (!verificationDelegates.Any()) yield break;

			foreach (var verificationDelegate in verificationDelegates)
			{
				// Get the asynchronous enumerator. This needs separate error handling, as getting the enumerator may throw before anything is enumerated at all.
				IAsyncEnumerator<string>? asyncEnumerator;
				try
				{
					asyncEnumerator = verificationDelegate.Invoke(cancellationToken).GetAsyncEnumerator(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (IocScopeVerificationException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new IocScopeVerificationException("An exception occurred while running a verification method. See the inner exception for further details.", ex);
				}

				// Start enumeration.
				var hasResult = true;
				while (hasResult)
				{
					string? result;
					try
					{
						hasResult = await asyncEnumerator.MoveNextAsync().ConfigureAwait(false);
						result = hasResult ? asyncEnumerator.Current : null;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (IocScopeVerificationException)
					{
						throw;
					}
					catch (Exception ex)
					{
						throw new IocScopeVerificationException("An exception occurred while running a verification method. See the inner exception for further details.", ex);
					}
					if (result is not null) yield return result;
				}
			}
		}
	}
}
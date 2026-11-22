namespace CallCenter.Application.Abstractions;

public interface IIranianCityRecognizer
{
    Task<IReadOnlyList<string>> RecognizeAsync(string transcript, CancellationToken cancellationToken);
}

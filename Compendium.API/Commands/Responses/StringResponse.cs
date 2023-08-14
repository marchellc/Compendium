namespace Compendium.Commands.Responses
{
    public class StringResponse : IResponse
    {
        private string _response;

        public bool IsContinued => false;
        public string FormulateString() => _response;

        public StringResponse(object response)
        {
            _response = response?.ToString() ?? "No response.";
        }
    }
}
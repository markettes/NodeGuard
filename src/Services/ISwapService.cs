public interface ISwapService
{
   public Task<SwapInResponse> CreateSwapInAsync(SwapInRequest request);
   public Task<SwapOutResponse> CreateSwapOutAsync(SwapOutRequest request);
   public Task<SwapInResponse> GetSwapInAsync(string swapId);
   public Task<SwapOutResponse> GetSwapOutAsync(string swapId);
}

public class SwapInRequest
{
   public ulong Amount { get; set; }
   public string Invoice { get; set; }
   public double? SatVb { get; set; }
   public string? RefundAddress { get; set; }
}

public class SwapInResponse
{
   public string Id { get; set; }
   public string Address { get; set; }
   public string TxId { get; set; }
   public ulong ExpectedAmount { get; set; }
   public uint TimeoutBlockHeight { get; set; }
}

public class SwapOutRequest
{
   public ulong Amount { get; set; }
   public string Address { get; set; }
   public double? SatVb { get; set; }
   public string[] ChanIds { get; set; }
   public ulong? RoutingFeeLimitPPM { get; set; }
}

public class SwapOutResponse
{
   public string Id { get; set; }
   public string LockUpAddress { get; set; }
   public ulong ExpectedAmount { get; set; }
}
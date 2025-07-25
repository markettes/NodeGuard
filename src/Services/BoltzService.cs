using Grpc.Net.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Grpc.Core;
using System.Text;
using Boltzrpc;

public class BoltzService : ISwapService
{
   private readonly ILogger<BoltzService> _logger;
   private readonly Boltz.BoltzClient _client;
   private readonly string? _macaroon;

   public BoltzService(ILogger<BoltzService> logger, string endpoint, string macaroon)
   {
      var httpHandler = new HttpClientHandler
      {
         ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      };

      var grpcChannel = GrpcChannel.ForAddress($"https://{endpoint}",
         new GrpcChannelOptions
         {
            HttpHandler = httpHandler,
            LoggerFactory = NullLoggerFactory.Instance,
         });

      _client = new Boltz.BoltzClient(grpcChannel);
      _macaroon = macaroon;
      _logger = logger;
   }

   private Metadata GetAuthMetadata()
   {
      var metadata = new Metadata();
      metadata.Add("macaroon", _macaroon);

      return metadata;
   }

   public async Task<SwapInResponse> CreateSwapInAsync(SwapInRequest request)
   {
      try
      {
         var grpcRequest = new CreateSwapRequest
         {
            Amount = request.Amount,
            Pair = new Pair { From = Currency.Btc, To = Currency.Btc }, // Submarine swap: BTC onchain to Lightning
            Invoice = request.Invoice,
            RefundAddress = request.RefundAddress
         };

         if (request.SatVb.HasValue)
         {
            grpcRequest.SatPerVbyte = request.SatVb.Value;
         }

         var response = await _client.CreateSwapAsync(grpcRequest, GetAuthMetadata());

         return new SwapInResponse
         {
            Id = response.Id,
            Address = response.Address,
            TxId = response.TxId,
            ExpectedAmount = response.ExpectedAmount,
            TimeoutBlockHeight = response.TimeoutBlockHeight
         };
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to create swap in");
         throw;
      }
   }

   public async Task<SwapOutResponse> CreateSwapOutAsync(SwapOutRequest request)
   {
      try
      {
         var grpcRequest = new CreateReverseSwapRequest
         {
            Amount = request.Amount,
            Address = request.Address,
            Pair = new Pair { From = Currency.Btc, To = Currency.Btc }, // Reverse swap: Lightning to BTC onchain
            RoutingFeeLimitPpm = request.RoutingFeeLimitPPM ?? 1000 // Default 0.1% routing fee limit
         };

         // Add channel IDs if provided
         if (request.ChanIds != null && request.ChanIds.Length > 0)
         {
            grpcRequest.ChanIds.AddRange(request.ChanIds);
         }

         var response = await _client.CreateReverseSwapAsync(grpcRequest, GetAuthMetadata());

         return new SwapOutResponse
         {
            Id = response.Id,
            LockUpAddress = response.LockupAddress,
            ExpectedAmount = request.Amount // The expected amount is what we want to receive
         };
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to create swap out");
         throw;
      }
   }

   public async Task<SwapInResponse> GetSwapInAsync(string swapId)
   {
      try
      {
         var request = new GetSwapInfoRequest
         {
            SwapId = swapId
         };

         var response = await _client.GetSwapInfoAsync(request, GetAuthMetadata());

         if (response.Swap != null)
         {
            return new SwapInResponse
            {
               Id = response.Swap.Id,
               Address = response.Swap.LockupAddress,
               TxId = response.Swap.LockupTransactionId ?? "",
               ExpectedAmount = response.Swap.ExpectedAmount,
               TimeoutBlockHeight = response.Swap.TimeoutBlockHeight
            };
         }

         throw new InvalidOperationException($"Swap with ID {swapId} not found or is not a submarine swap");
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to get swap in with ID {SwapId}", swapId);
         throw;
      }
   }

   public async Task<SwapOutResponse> GetSwapOutAsync(string swapId)
   {
      try
      {
         var request = new GetSwapInfoRequest
         {
            SwapId = swapId
         };

         var response = await _client.GetSwapInfoAsync(request, GetAuthMetadata());

         if (response.ReverseSwap != null)
         {
            return new SwapOutResponse
            {
               Id = response.ReverseSwap.Id,
               LockUpAddress = response.ReverseSwap.ClaimAddress,
               ExpectedAmount = response.ReverseSwap.OnchainAmount
            };
         }

         throw new InvalidOperationException($"Swap with ID {swapId} not found or is not a reverse swap");
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to get swap out with ID {SwapId}", swapId);
         throw;
      }
   }
}
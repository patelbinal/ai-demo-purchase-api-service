# Purchase Service - Automotive Marketplace Microservice

This is the Purchase Service microservice for the automotive marketplace platform, built with ASP.NET Core 8.0 and PostgreSQL.

## Features

- **CRUD Operations**: Complete Create, Read, Update, Delete operations for purchases
- **PostgreSQL Integration**: Entity Framework Core with PostgreSQL database
- **Dockerized**: Ready for containerized deployment
- **Kubernetes Support**: Includes Kubernetes deployment manifests
- **API Documentation**: Swagger/OpenAPI documentation
- **Filtering & Pagination**: Support for filtering by buyer, offer, status with pagination
- **Error Handling**: Comprehensive error handling and logging

## Architecture

### Purchase Entity
- `PurchaseId`: Unique identifier
- `BuyerId`: Reference to the buyer
- `OfferId`: Reference to the vehicle offer
- `PurchaseDate`: When the purchase was made
- `Amount`: Purchase amount (decimal with precision)
- `Status`: Purchase status (Pending, Completed, Cancelled, etc.)
- `BuyerDetails`: JSON object containing buyer information:
  - `Name`: Buyer's full name
  - `Email`: Buyer's email address
  - `Phone`: Buyer's phone number
  - `Address`: Buyer's address (street, city, state, zipCode, country)
  - `PaymentMethod`: Payment method used
- `CreatedAt` / `UpdatedAt`: Timestamps

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/purchases` | Get all purchases (with filtering) |
| GET | `/api/purchases/{id}` | Get purchase by ID |
| POST | `/api/purchases` | Create new purchase |
| PUT | `/api/purchases/{id}` | Update existing purchase |
| DELETE | `/api/purchases/{id}` | Delete purchase |
| GET | `/api/purchases/buyer/{buyerId}` | Get purchases by buyer |
| GET | `/api/purchases/offer/{offerId}` | Get purchases by offer |

### Query Parameters (GET /api/purchases)
- `buyerId`: Filter by buyer ID
- `offerId`: Filter by offer ID
- `status`: Filter by purchase status
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10)

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- PostgreSQL (if running locally)

### Option 1: Docker Compose (Recommended)

1. **Clone and navigate to the project:**
   ```bash
   cd ai_demo_purchase
   ```

2. **Run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

3. **Access the service:**
   - API: http://localhost:8001/api/purchases
   - Swagger UI: http://localhost:8001/swagger

### Option 2: Local Development

1. **Setup PostgreSQL database:**
   ```bash
   docker run --name postgres-dev -e POSTGRES_DB=purchasedb -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15-alpine
   ```

2. **Run the service:**
   ```bash
   cd PurchaseService
   dotnet restore
   dotnet run
   ```

3. **Access the service:**
   - API: http://localhost:5000/api/purchases
   - Swagger UI: http://localhost:5000/swagger

### Option 3: Kubernetes Deployment

1. **Build Docker image:**
   ```bash
   docker build -t purchase-service:latest .
   ```

2. **Apply Kubernetes manifests:**
   ```bash
   kubectl apply -f k8s/purchase-service.yaml
   ```

3. **Access the service:**
   ```bash
   kubectl port-forward service/purchase-service 8001:8001
   ```

## Testing the API

### Create a Purchase
```bash
curl -X POST http://localhost:8001/api/purchases \
  -H "Content-Type: application/json" \
  -d '{
    "buyerId": 1001,
    "offerId": 2001,
    "purchaseDate": "2026-01-02T10:00:00Z",
    "amount": 25000.00,
    "status": "Pending",
    "buyerDetails": {
      "name": "John Doe",
      "email": "john.doe@email.com",
      "phone": "+1-555-0123",
      "address": {
        "street": "123 Main St",
        "city": "Springfield",
        "state": "IL",
        "zipCode": "62701",
        "country": "USA"
      },
      "paymentMethod": "Credit Card"
    }
  }'
```

### Get All Purchases
```bash
curl -X GET http://localhost:8001/api/purchases
```

### Get Purchases by Buyer
```bash
curl -X GET http://localhost:8001/api/purchases/buyer/1001
```

### Filter Purchases
```bash
curl -X GET "http://localhost:8001/api/purchases?status=Pending&page=1&pageSize=5"
```

## Automation Scripts

### Windows
```bash
scripts\run-purchase-service.bat
```

### Linux/Mac
```bash
chmod +x scripts/run-purchase-service.sh
./scripts/run-purchase-service.sh
```

## Database Schema

The service automatically creates the database schema on startup. Key features:
- Auto-incrementing PurchaseId
- Indexes on BuyerId, OfferId, and Status for performance
- Decimal precision for monetary amounts
- Timestamps with UTC defaults

## Configuration

### Connection Strings
- **Development**: Uses localhost PostgreSQL
- **Production**: Uses postgres-db service name for Docker/K8s

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to "Production" for containerized deployments
- `ASPNETCORE_URLS`: Configure listening URLs

## Error Handling

The service includes comprehensive error handling:
- Model validation with data annotations
- Try-catch blocks with detailed logging
- Appropriate HTTP status codes
- User-friendly error messages

## Integration with Other Services

This Purchase Service is designed to integrate with:
- **Offer Service**: References OfferId for vehicle offers
- **Search Service**: Provides purchase data for centralized search
- **Transport Service**: Purchases trigger transport requests

## Next Steps

1. **Implement Event Publishing**: Add events for purchase lifecycle
2. **Add Authentication**: Implement role-based access control
3. **Add Integration Tests**: Comprehensive API testing
4. **Monitoring**: Add health checks and metrics
5. **Load Testing**: Performance testing scripts

## Contributing

1. Follow .NET coding conventions
2. Add unit tests for new features
3. Update API documentation
4. Test with Docker before submitting

## License

This project is part of the automotive marketplace microservices architecture.
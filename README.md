# Invoice Chatbot with RAG System

## 📋 Project Overview

The **Invoice Chatbot with RAG (Retrieval-Augmented Generation) System** is an intelligent conversational AI application designed to provide natural language querying capabilities for invoice management systems. The chatbot leverages a hybrid approach combining rule-based intent recognition with Large Language Model (LLM) integration to deliver accurate and contextual responses about invoice data.

### 🎯 Key Features

- **Bilingual Support**: Arabic and English language support
- **Natural Language Processing**: Understands complex queries in natural language
- **RAG Integration**: Combines database retrieval with LLM generation
- **Real-time Invoice Queries**: Get instant insights about invoices, customers, and financial data
- **Intent Recognition**: Smart detection of user intentions (totals, counts, customer queries, etc.)
- **Modern Web Interface**: Angular-based responsive frontend
- **RESTful API**: Clean, scalable backend architecture

## 🏗️ Architecture Overview

The project follows a **Clean Architecture** pattern with clear separation of concerns across multiple layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (Angular)                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   Chat UI       │  │  Invoice UI     │  │  Services    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Backend API (.NET 9)                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │  Controllers    │  │   Services      │  │  Repository  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Data Layer                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │   Entity Models │  │   DbContext     │  │  Migrations  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Database (MySQL)                         │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

### Backend Solution (`InvoiceChatbotSolution/`)

```
InvoiceChatbotSolution/
├── Chatbot.API/                    # Web API Layer
│   ├── Controllers/                # API Controllers
│   │   ├── ChatController.cs       # Chat endpoint
│   │   └── InvoiceController.cs    # Invoice CRUD operations
│   ├── Program.cs                  # Application entry point
│   └── appsettings.json           # Configuration
│
├── Chatbot.DTOs/                   # Data Transfer Objects
│   ├── ApiResponse/                # API response models
│   ├── InvoiceDetailsDTO/          # Invoice details DTOs
│   ├── InvoiceDTO/                 # Invoice DTOs
│   └── UserMessagesDTO/            # Chat message DTOs
│
├── Chatbot.Service/                # Business Logic Layer
│   ├── InvoiceService/             # Invoice business logic
│   └── RagService/                 # RAG system implementation
│       ├── IRagService.cs          # RAG service interface
│       └── RagService_Nochaching.cs # Main RAG implementation
│
├── Chatbot.Repository/             # Data Access Layer
│   ├── BaseRepository/             # Generic repository pattern
│   └── InvoiceRepository/          # Invoice-specific repository
│
├── Data/                          # Entity Framework Layer
│   ├── ChatbotContext.cs          # DbContext
│   └── Migrations/                # Database migrations
│
└── Models/                        # Domain Models
    ├── Enums/                     # Enumerations
    │   └── InvoiceStatus.cs       # Invoice status enum
    └── Models/                    # Entity models
        ├── BaseEntity.cs          # Base entity with audit fields
        ├── Invoice/               # Invoice entity
        └── InvoiceDetails/        # Invoice details entity
```

### Frontend Application (`Front-Chatbot/`)

```
Front-Chatbot/Front-Chatbot/
├── src/
│   ├── app/
│   │   ├── core/                  # Core functionality
│   │   │   ├── models/            # TypeScript interfaces
│   │   │   │   ├── Chat_models.ts # Chat-related models
│   │   │   │   └── invoice_models.ts # Invoice models
│   │   │   └── services/          # Angular services
│   │   │       ├── api_service.ts # Base API service
│   │   │       ├── chat_service.ts # Chat service
│   │   │       └── invoice_service.ts # Invoice service
│   │   ├── layouts/               # Layout components
│   │   │   ├── header/            # Header component
│   │   │   └── Float-support/     # Floating support component
│   │   └── pages/                 # Page components
│   │       ├── Chatbot/           # Chat interface
│   │       └── Invoice/           # Invoice management
│   ├── environments/              # Environment configuration
│   └── styles.css                 # Global styles
├── package.json                   # Dependencies
└── angular.json                   # Angular configuration
```

## 🤖 RAG System Implementation

### How It Works

The RAG (Retrieval-Augmented Generation) system combines two approaches:

1. **Rule-Based Intent Recognition**: Pre-defined patterns for common queries
2. **LLM Integration**: Fallback to Mistral LLM for complex queries

### Intent Recognition System

The system recognizes various intents through keyword matching:

```csharp
// Example intent patterns
- Total queries: "اجمالي", "مجموع", "total", "sum"
- Count queries: "عدد", "كم", "count", "how many"
- Customer queries: "عميل", "زبون", "customer"
- Time-based queries: "شهر", "أيام", "month", "days"
- Status queries: "متأخرة", "غير مدفوعة", "overdue", "unpaid"
```

### Query Processing Flow

```
User Query → Language Detection → Intent Matching → Database Query → Response Generation
     ↓              ↓                ↓              ↓              ↓
Natural Language → Arabic/English → Rule-based → MySQL Query → Formatted Response
```

### LLM Integration

When rule-based matching fails, the system:

1. **Builds Context**: Creates a prompt with database schema and sample data
2. **Calls Mistral LLM**: Sends structured prompt to local Mistral instance
3. **Generates Response**: Returns contextual, accurate answers

## 🚀 Getting Started

### Prerequisites

- **.NET 9 SDK**
- **Angular 20 CLI**
- **MySQL Server**
- **Mistral LLM** (running on localhost:11434)

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Chatbot/InvoiceChatbotSolution
   ```

2. **Configure Database**
   - Update connection string in `Chatbot.API/appsettings.json`
   - Run migrations:
   ```bash
   cd Data
   dotnet ef database update
   ```

3. **Start the API**
   ```bash
   cd Chatbot.API
   dotnet run
   ```

### Frontend Setup

1. **Install dependencies**
   ```bash
   cd Front-Chatbot/Front-Chatbot
   npm install
   ```

2. **Configure API URL**
   - Update `src/environments/environment.ts` with your API URL

3. **Start the application**
   ```bash
   ng serve
   ```

### Mistral LLM Setup

1. **Install Ollama** (for running Mistral locally)
2. **Pull Mistral model**:
   ```bash
   ollama pull mistral
   ```
3. **Start Ollama service**:
   ```bash
   ollama serve
   ```

## 💬 Usage Examples

### Arabic Queries
- "ما هو إجمالي الفواتير لهذا الشهر؟"
- "كم عدد الفواتير للعميل أحمد؟"
- "أعطني ملخص الفاتورة رقم INV-001"
- "ما هي الفواتير المتأخرة؟"

### English Queries
- "What is the total value of invoices this month?"
- "How many invoices does customer John have?"
- "Show me summary for invoice INV-001"
- "Which invoices are overdue?"

## 🔧 Technical Features

### Backend Features

- **Onion Architecture**: Separation of concerns with clear layers
- **Repository Pattern**: Generic repository for data access
- **Dependency Injection**: Proper service registration
- **Entity Framework Core**: ORM with MySQL provider
- **CORS Configuration**: Cross-origin resource sharing
- **Swagger Documentation**: API documentation

### Frontend Features

- **Angular 20**: Modern frontend framework
- **TypeScript**: Type-safe development
- **Responsive Design**: Mobile-friendly interface
- **Service Layer**: Clean API communication
- **Error Handling**: Comprehensive error management

### Database Design

- **BaseEntity**: Audit fields (CreatedAt, UpdatedAt, IsDeleted)
- **Invoice**: Main invoice entity with relationships
- **InvoiceDetails**: Line items for each invoice
- **Soft Delete**: Logical deletion support

## 🛠️ Development Guidelines

### Code Quality

- **Clean Code Principles**: Readable, maintainable code
- **SOLID Principles**: Single responsibility, dependency inversion
- **Naming Conventions**: Clear, descriptive names
- **Error Handling**: Comprehensive exception management

### Performance Considerations

- **Async/Await**: Non-blocking operations
- **Database Optimization**: Efficient queries with proper indexing
- **Caching Strategy**: LLM response caching (planned)
- **Connection Pooling**: Database connection management

## 🔮 Future Enhancements

- [ ] **Response Caching**: Cache LLM responses for better performance
- [ ] **Advanced Analytics**: Invoice trend analysis
- [ ] **Multi-language Support**: Additional languages
- [ ] **Real-time Notifications**: WebSocket integration
- [ ] **Export Functionality**: PDF/Excel export
- [ ] **User Authentication**: Role-based access control

## 📝 API Documentation

### Chat Endpoint

```http
POST /api/chat
Content-Type: application/json

{
  "message": "What is the total value of invoices?",
  "language": "en"
}
```

### Invoice Endpoints

```http
GET    /api/invoice          # Get all invoices
GET    /api/invoice/{id}     # Get invoice by ID
POST   /api/invoice          # Create new invoice
PUT    /api/invoice/{id}     # Update invoice
DELETE /api/invoice/{id}     # Delete invoice
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Team

- **Backend Development**: .NET 9, Entity Framework Core
- **Frontend Development**: Angular 20, TypeScript
- **Database Design**: MySQL, EF Core Migrations
- **AI Integration**: Ollama, Mistral LLM, RAG System

---

**Note**: This project demonstrates modern software development practices with AI integration, making it an excellent example for learning clean architecture, RAG systems, and full-stack development. 

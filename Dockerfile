# --- Estágio 1: O "Build" (Compilação) ---
# Usamos a imagem "sdk" para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia e restaura o .csproj
COPY backend/UsinaApi/UsinaApi.csproj ./backend/UsinaApi/
RUN dotnet restore ./backend/UsinaApi/UsinaApi.csproj

# Copia o resto do código e publica
COPY . .
RUN dotnet publish ./backend/UsinaApi/UsinaApi.csproj -c Release -o /app/publish

# --- Estágio 2: O "Runtime" (Execução) ---
# Usamos a imagem "aspnet", que é pequena, segura e otimizada
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O ÚNICO comando necessário: rodar a DLL.
# O Program.cs dentro da DLL vai tratar da migração do banco.
ENTRYPOINT ["sh", "-c", "echo '--- INICIANDO DIAGNÓSTICO ---' && echo 'Connection String: $ConnectionStrings__DefaultConnection' && echo 'JWT Key: $Jwt__Key' && echo '--- FIM DO DIAGNÓSTICO, APP EM PAUSA ---' && sleep 3600"]
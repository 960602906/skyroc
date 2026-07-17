# 路由 SQL 文件使用说明

## 方法一：使用 docker exec 直接执行（推荐）

如果您的 PostgreSQL 容器正在运行，可以使用以下命令：

```bash
# 方式 1：通过标准输入执行
docker exec -i <容器名称或ID> psql -U <用户名> -d <数据库名> < routes.sql

# 示例（假设容器名为 postgres，用户名为 postgres，数据库名为 mydb）
docker exec -i postgres psql -U postgres -d mydb < routes.sql
```

## 方法二：先复制文件到容器再执行

```bash
# 1. 复制 SQL 文件到容器
docker cp routes.sql <容器名称或ID>:/tmp/routes.sql

# 2. 在容器内执行 SQL 文件
docker exec -it <容器名称或ID> psql -U <用户名> -d <数据库名> -f /tmp/routes.sql

# 示例
docker cp routes.sql postgres:/tmp/routes.sql
docker exec -it postgres psql -U postgres -d mydb -f /tmp/routes.sql
```

## 方法三：使用外部 psql 客户端连接执行

如果您在本地安装了 PostgreSQL 客户端：

```bash
# 需要先知道容器的端口映射（通常是 5432）
psql -h localhost -p <映射端口> -U <用户名> -d <数据库名> -f routes.sql

# 示例（假设端口映射为 5432）
psql -h localhost -p 5432 -U postgres -d mydb -f routes.sql
```

## 方法四：在 docker-compose 中使用初始化脚本

如果您使用 docker-compose，可以将 SQL 文件挂载到容器的初始化目录：

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:latest
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
      POSTGRES_DB: mydb
    volumes:
      - ./routes.sql:/docker-entrypoint-initdb.d/routes.sql
    ports:
      - "5432:5432"
```

**注意**：这种方式只在容器首次启动时执行，如果数据库已存在则不会执行。

## 方法五：在容器启动后手动执行

```bash
# 1. 进入容器
docker exec -it <容器名称或ID> bash

# 2. 在容器内执行
psql -U <用户名> -d <数据库名> -f /path/to/routes.sql

# 或者直接执行 SQL 内容
psql -U <用户名> -d <数据库名> << EOF
$(cat routes.sql)
EOF
```

## 验证数据

执行完成后，可以验证数据是否插入成功：

```bash
# 查看表结构
docker exec -it <容器名称或ID> psql -U <用户名> -d <数据库名> -c "\d routes"

# 查看数据条数
docker exec -it <容器名称或ID> psql -U <用户名> -d <数据库名> -c "SELECT COUNT(*) FROM routes;"

# 查看前几条数据
docker exec -it <容器名称或ID> psql -U <用户名> -d <数据库名> -c "SELECT id, name, path, title FROM routes LIMIT 5;"
```

## 常见问题

### 1. 权限问题
如果遇到权限错误，可能需要指定密码：
```bash
# 使用环境变量
PGPASSWORD=your_password docker exec -i postgres psql -U postgres -d mydb < routes.sql

# 或使用交互式输入
docker exec -it postgres psql -U postgres -d mydb
# 然后输入密码，再执行 \i /path/to/routes.sql
```

### 2. 字符编码问题
确保 SQL 文件使用 UTF-8 编码：
```bash
# 检查文件编码
file routes.sql
```

### 3. 表已存在
如果表已存在，SQL 中的 `CREATE TABLE IF NOT EXISTS` 会跳过创建，直接插入数据。
如果需要重新创建，可以先删除表：
```bash
docker exec -it <容器名称或ID> psql -U <用户名> -d <数据库名> -c "DROP TABLE IF EXISTS routes CASCADE;"
```

## 快速执行脚本

您也可以创建一个简单的 shell 脚本来执行：

```bash
#!/bin/bash
# execute-routes-sql.sh

CONTAINER_NAME="postgres"  # 修改为您的容器名称
DB_USER="postgres"          # 修改为您的数据库用户名
DB_NAME="mydb"              # 修改为您的数据库名称

echo "正在执行 routes.sql..."
docker exec -i $CONTAINER_NAME psql -U $DB_USER -d $DB_NAME < routes.sql

if [ $? -eq 0 ]; then
    echo "✅ SQL 文件执行成功！"
    echo "正在验证数据..."
    docker exec -it $CONTAINER_NAME psql -U $DB_USER -d $DB_NAME -c "SELECT COUNT(*) as total_routes FROM routes;"
else
    echo "❌ SQL 文件执行失败！"
    exit 1
fi
```

使用方法：
```bash
chmod +x execute-routes-sql.sh
./execute-routes-sql.sh
```



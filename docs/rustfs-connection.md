# RustFS 连接与使用说明

本机已部署 **RustFS**（S3 兼容对象存储，Rust 实现），可用于项目文件上传 / 下载。

## 服务地址

| 用途 | 地址 |
|------|------|
| API（S3）本机 | `http://127.0.0.1:9000` |
| API（S3）内网 | `http://192.168.132.152:9000` |
| API（S3）公网 | `http://115.190.128.7:9000` |
| Web 控制台本机 | `http://127.0.0.1:9001` |
| Web 控制台内网 | `http://192.168.132.152:9001` |
| Web 控制台公网 | `http://115.190.128.7:9001` |

> 外网访问需在云安全组放行 **9000**、**9001**。同机应用优先使用 `127.0.0.1`。

## 账号与存储

| 项 | 值 |
|----|-----|
| Access Key | `rustfsadmin123` |
| Secret Key | `RustFS@Secret2026` |
| Region | `us-east-1`（可随意填写，RustFS 一般不校验） |
| Path Style | **必须开启**（`ForcePathStyle = true`） |
| 数据目录 | `/data/rustfs0` |
| 配置文件 | `/etc/default/rustfs` |
| 日志目录 | `/var/log/rustfs/` |
| 二进制路径 | `/usr/local/bin/rustfs` |

> **安全提示**：当前密钥为安装时临时设置，生产环境请尽快修改 `/etc/default/rustfs` 中的 `RUSTFS_ACCESS_KEY` / `RUSTFS_SECRET_KEY`，且不能使用 `rustfsadmin` 作为 Access Key。

## Web 控制台登录

1. 浏览器打开：`http://127.0.0.1:9001`（或内网 / 公网地址）
2. Access Key：`rustfsadmin123`
3. Secret Key：`RustFS@Secret2026`
4. 可在控制台中创建 Bucket、上传文件、管理对象与权限

## 应用连接配置（通用）

```text
Endpoint:        http://127.0.0.1:9000
AccessKey:       rustfsadmin123
SecretKey:       RustFS@Secret2026
BucketName:      （自行创建，如 skyroc）
Region:          us-east-1
ForcePathStyle:  true
SSL / HTTPS:     false（当前为 HTTP）
```

### appsettings.json 示例

SkyRoc 仓库内 `SkyRoc/appsettings.json` 只提交 Endpoint / Bucket / Region 等非密钥项；`AccessKey` / `SecretKey` 必须用环境变量注入（`RustFS__AccessKey`、`RustFS__SecretKey`）。自动化测试使用 `RustFS:UseInMemory=true`，不连接本机 RustFS。

```json
{
  "RustFS": {
    "Endpoint": "http://127.0.0.1:9000",
    "BucketName": "skyroc",
    "Region": "us-east-1",
    "ForcePathStyle": true,
    "UseSsl": false
  }
}
```

```powershell
$env:RustFS__AccessKey = "rustfsadmin123"
$env:RustFS__SecretKey = "RustFS@Secret2026"
```

### .NET（AWSSDK.S3）示例

```csharp
using Amazon.S3;
using Amazon.S3.Model;

var config = new AmazonS3Config
{
    ServiceURL = "http://127.0.0.1:9000",
    ForcePathStyle = true,
    AuthenticationRegion = "us-east-1"
};

using var client = new AmazonS3Client("rustfsadmin123", "RustFS@Secret2026", config);

// 创建 Bucket（若不存在）
await client.PutBucketAsync(new PutBucketRequest { BucketName = "skyroc" });

// 上传
await client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "skyroc",
    Key = "demo/hello.txt",
    ContentBody = "hello rustfs"
});

// 获取预签名 URL
var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
{
    BucketName = "skyroc",
    Key = "demo/hello.txt",
    Expires = DateTime.UtcNow.AddHours(1)
});
```

### Python（boto3）示例

```bash
pip install boto3
```

```python
import boto3
from botocore.client import Config

s3 = boto3.client(
    "s3",
    endpoint_url="http://127.0.0.1:9000",
    aws_access_key_id="rustfsadmin123",
    aws_secret_access_key="RustFS@Secret2026",
    region_name="us-east-1",
    config=Config(signature_version="s3v4", s3={"addressing_style": "path"}),
)

# 创建 Bucket
s3.create_bucket(Bucket="skyroc")

# 上传
s3.put_object(Bucket="skyroc", Key="demo/hello.txt", Body=b"hello rustfs")

# 列出对象
for obj in s3.list_objects_v2(Bucket="skyroc").get("Contents", []):
    print(obj["Key"])
```

### Java（AWS SDK v2）要点

- `endpointOverride(URI.create("http://127.0.0.1:9000"))`
- `forcePathStyle(true)`
- credentials：`rustfsadmin123` / `RustFS@Secret2026`

## 本机 mc 命令

本机已安装 MinIO Client（`mc`），同样兼容 RustFS S3 API。建议单独配置别名 `rustfs`：

```bash
mc alias set rustfs http://127.0.0.1:9000 rustfsadmin123 'RustFS@Secret2026'

# 列出 Bucket
mc ls rustfs/

# 新建 Bucket
mc mb rustfs/skyroc

# 上传文件
mc cp ./myfile.pdf rustfs/skyroc/docs/myfile.pdf

# 下载文件
mc cp rustfs/skyroc/docs/myfile.pdf ./myfile.pdf

# 查看对象信息
mc stat rustfs/skyroc/docs/myfile.pdf
```

> 注意：本机 `mc` 别名 `local` 仍指向原 MinIO 配置；使用 RustFS 时请用 `rustfs` 别名，或更新 `local` 的凭证。

## AWS CLI 示例

```bash
aws configure set aws_access_key_id rustfsadmin123
aws configure set aws_secret_access_key 'RustFS@Secret2026'
aws configure set default.region us-east-1

# 创建 Bucket
aws --endpoint-url http://127.0.0.1:9000 s3 mb s3://skyroc

# 上传 / 下载
aws --endpoint-url http://127.0.0.1:9000 s3 cp ./file.txt s3://skyroc/file.txt
aws --endpoint-url http://127.0.0.1:9000 s3 ls s3://skyroc/
```

## 服务管理

```bash
# 查看状态
systemctl status rustfs

# 启动 / 停止 / 重启
systemctl start rustfs
systemctl stop rustfs
systemctl restart rustfs

# 查看日志
journalctl -u rustfs -f

# 查看 RustFS 版本与系统信息
/usr/local/bin/rustfs --version
/usr/local/bin/rustfs info
```

## 修改配置

编辑 `/etc/default/rustfs`，常用环境变量：

| 变量 | 说明 | 当前值 |
|------|------|--------|
| `RUSTFS_ACCESS_KEY` | S3 Access Key | `rustfsadmin123` |
| `RUSTFS_SECRET_KEY` | S3 Secret Key | `RustFS@Secret2026` |
| `RUSTFS_VOLUMES` | 数据存储目录 | `/data/rustfs0` |
| `RUSTFS_ADDRESS` | API 监听地址 | `:9000` |
| `RUSTFS_CONSOLE_ADDRESS` | 控制台监听地址 | `:9001` |
| `RUSTFS_CONSOLE_ENABLE` | 是否启用控制台 | `true` |
| `RUSTFS_OBS_LOG_DIRECTORY` | 日志目录 | `/var/log/rustfs/` |

修改后重启服务：

```bash
systemctl restart rustfs
```

## 安装说明（本机）

- 安装方式：官方脚本 + **MUSL 静态编译二进制**
- 原因：Ubuntu 22.04（glibc 2.35）无法运行官方 GNU 版（需 glibc 2.38+）
- MUSL 包地址：`https://dl.rustfs.com/artifacts/rustfs/release/rustfs-linux-x86_64-musl-latest.zip`
- systemd 单元：`/etc/systemd/system/rustfs.service`

升级时请下载 MUSL 版本替换 `/usr/local/bin/rustfs`，勿直接使用官方脚本默认的 GNU 包。

## 注意事项

1. **Path Style 必开**：否则部分 SDK 会按虚拟主机风格访问，导致连不上。
2. 当前为 **HTTP**，仅适合内网 / 受控环境；公网建议前面加 Nginx 反代并启用 HTTPS。
3. 本机 MinIO 已停用，RustFS 占用 **9000 / 9001** 端口；若需同时运行，请修改 RustFS 或 MinIO 的端口配置。
4. 数据目录不可放在 `/tmp` 下（systemd `PrivateTmp=true` 会导致 RustFS 无法访问）。
5. 磁盘空间见 `df -h /data/rustfs0`；当前系统约 2GB 内存，适合轻量对象存储场景。

## 参考链接

- 官方文档：https://docs.rustfs.com
- GitHub：https://github.com/rustfs/rustfs
- 客户端示例：https://github.com/rustfs/example

import { readFile, readdir } from 'node:fs/promises';
import { extname, join, relative } from 'node:path';
import process from 'node:process';
import { fileURLToPath } from 'node:url';

const sourceRoot = fileURLToPath(new URL('../src/', import.meta.url));
const scanRoots = [join(sourceRoot, 'pages'), join(sourceRoot, 'service', 'hooks')];
const forbiddenPatterns = [
  {
    expression: /fetchGetAll(?:CustomerProtocols|Customers|Goods|GoodsUnits|Quotations|Suppliers)\s*\(/,
    message: '禁止为高基数下拉读取全量业务数据'
  },
  { expression: /fetchGetOrderList\s*\(\s*\{[^}]*size\s*:\s*100/s, message: '禁止用固定大页模拟订单下拉' }
];

async function collectFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(
    entries.map(entry => {
      const path = join(directory, entry.name);
      return entry.isDirectory() ? collectFiles(path) : [path];
    })
  );
  return files.flat();
}

const files = (await Promise.all(scanRoots.map(collectFiles)))
  .flat()
  .filter(path => ['.ts', '.tsx'].includes(extname(path)));
const violations = [];
const sources = await Promise.all(files.map(async file => [file, await readFile(file, 'utf8')]));

for (const [file, source] of sources) {
  for (const rule of forbiddenPatterns) {
    if (rule.expression.test(source)) violations.push(`${relative(sourceRoot, file)}: ${rule.message}`);
  }
}

if (violations.length) {
  console.error(violations.join('\n'));
  process.exitCode = 1;
}

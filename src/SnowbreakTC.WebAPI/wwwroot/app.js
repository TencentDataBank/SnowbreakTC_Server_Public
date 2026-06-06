const $ = (s) => document.querySelector(s);
const $$ = (s) => [...document.querySelectorAll(s)];

const state = {
  tab: "overview",
  token: localStorage.getItem("stc_admin_token") || "",
  selectedUid: null,
  history: [],
  historyIdx: -1,
  setupData: null,
};

const titles = {
  overview: "概览",
  accounts: "账号与玩家",
  grant: "物品与发放",
  console: "GM 控制台",
  setup: "接入与网络",
};

function esc(v) {
  return String(v ?? "").replace(/[&<>"']/g, (c) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;",
  }[c]));
}

function fmtUptime(sec) {
  const s = Number(sec) || 0;
  const d = Math.floor(s / 86400);
  const h = Math.floor((s % 86400) / 3600);
  const m = Math.floor((s % 3600) / 60);
  const r = s % 60;
  return (d ? `${d}d ` : "") + `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}:${String(r).padStart(2, "0")}`;
}

function fmtTime(unix) {
  if (!unix) return "-";
  return new Date(unix * 1000).toLocaleString();
}

function toast(msg, err = false) {
  const el = document.createElement("div");
  el.className = `toast${err ? " err" : ""}`;
  el.textContent = msg;
  $("#toast-wrap").appendChild(el);
  setTimeout(() => el.remove(), 3200);
}

async function api(path, opts = {}) {
  const headers = { ...(opts.headers || {}) };
  if (state.token) headers["Authorization"] = `Bearer ${state.token}`;
  if (opts.body && !headers["Content-Type"]) headers["Content-Type"] = "application/json";

  const res = await fetch(path, { ...opts, headers });
  if (res.status === 401) {
    $("#auth-modal").classList.remove("hidden");
    throw new Error("需要管理员 Token");
  }
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.message || data.error || `HTTP ${res.status}`);
  return data;
}

async function runGm(command, echo = false) {
  if (echo) appendLog(`> ${command}`, "log-cmd");
  const res = await api("/api/console", {
    method: "POST",
    body: JSON.stringify({ command }),
  });
  const lines = res.messages || [];
  if (echo) {
    lines.forEach((m) => appendLog(m, res.success ? "" : "log-err"));
  }
  return res;
}

function appendLog(text, cls = "") {
  const log = $("#console-log");
  const line = document.createElement("div");
  if (cls) line.className = cls;
  line.textContent = text;
  log.appendChild(line);
  log.scrollTop = log.scrollHeight;
}

function switchTab(tab) {
  state.tab = tab;
  $$(".nav-btn").forEach((b) => b.classList.toggle("active", b.dataset.tab === tab));
  $$(".panel").forEach((p) => p.classList.toggle("active", p.id === `tab-${tab}`));
  $("#page-title").textContent = titles[tab] || tab;
  refreshActive();
}

async function loadOverview() {
  const [ov, on] = await Promise.all([api("/api/overview"), api("/api/online")]);
  $("#brand-name").textContent = ov.server_name || "SnowbreakTC";
  const st = $("#brand-status");
  st.textContent = "运行中";
  st.className = "brand-sub ok";

  $("#stat-online").textContent = ov.online ?? 0;
  $("#stat-accounts").textContent = ov.accounts ?? 0;
  $("#stat-apis").textContent = ov.commands ?? 0;
  $("#stat-uptime").textContent = fmtUptime(ov.uptime_sec);

  const players = on.players || [];
  $("#online-summary").textContent = `${on.count ?? players.length} 人在线`;
  $("#online-tbody").innerHTML = players.length
    ? players.map((p) => `
      <tr>
        <td class="mono">${esc(p.uid)}</td>
        <td>${esc(p.name)}</td>
        <td class="mono">Lv.${esc(p.level)}</td>
        <td class="right">
          <button class="btn small secondary" onclick="inspectPlayer(${Number(p.uid)})">详情</button>
          <button class="btn small danger" onclick="kickUid(${Number(p.uid)})">踢出</button>
        </td>
      </tr>`).join("")
    : `<tr><td colspan="4" class="muted">当前没有在线玩家</td></tr>`;
}

async function loadAccounts() {
  const q = $("#search-q").value.trim();
  const [accounts, currencies] = await Promise.all([
    api(`/api/accounts${q ? `?q=${encodeURIComponent(q)}` : ""}`),
    api("/api/currencies"),
  ]);
  const curMap = new Map(currencies.map((c) => [c.uid, c]));
  $("#accounts-count").textContent = `${accounts.length} 条`;
  $("#accounts-tbody").innerHTML = accounts.length
    ? accounts.map((a) => {
      const c = curMap.get(a.uid);
      return `
      <tr class="clickable" onclick="showPlayer(${Number(a.uid)})">
        <td class="mono">${esc(a.uid)}</td>
        <td>${esc(a.username)}</td>
        <td class="muted">${esc(a.email)}</td>
        <td class="mono">${c ? `Lv.${esc(c.level)}` : "-"}</td>
        <td class="muted">${esc(fmtTime(a.created_at))}</td>
        <td class="right">
          <button class="btn small danger" onclick="event.stopPropagation(); deletePlayerData(${Number(a.uid)})">清档</button>
        </td>
      </tr>`;
    }).join("")
    : `<tr><td colspan="6" class="muted">暂无匹配账号</td></tr>`;

  if (state.selectedUid) await showPlayer(state.selectedUid, currencies);
}

async function showPlayer(uid, cachedCurrencies = null) {
  state.selectedUid = uid;
  const [info, currencies] = await Promise.all([
    api(`/api/players/${uid}`),
    cachedCurrencies ? Promise.resolve(cachedCurrencies) : api("/api/currencies"),
  ]);
  const cur = currencies.find((c) => c.uid === uid);
  const badge = $("#detail-status");
  badge.classList.remove("hidden");
  badge.textContent = info.online ? "在线" : "离线";
  badge.className = `badge${info.online ? " online" : ""}`;

  const moneyRows = Object.entries(cur?.money || {});
  $("#player-detail-body").innerHTML = `
    <div class="kv-list">
      <div class="kv-row"><span class="muted">UID</span><span class="mono">${esc(info.uid)}</span></div>
      <div class="kv-row"><span class="muted">昵称</span><span>${esc(info.name)}</span></div>
      <div class="kv-row"><span class="muted">等级 / 经验</span><span class="mono">Lv.${esc(info.level)} / ${esc(info.exp)}</span></div>
      <div class="kv-row"><span class="muted">体力</span><span class="mono">${esc(info.vigor)}</span></div>
      <div class="kv-row"><span class="muted">背包条目数</span><span class="mono">${esc(info.items)}</span></div>
    </div>
    ${moneyRows.length ? `<div class="muted" style="margin-top:10px">货币</div><div class="kv-list">${moneyRows.map(([k, v]) => `<div class="kv-row"><span class="mono">${esc(k)}</span><span class="mono">${esc(v)}</span></div>`).join("")}</div>` : ""}
    <div class="btn-row" style="margin-top:12px">
      ${info.online ? `<button class="btn small danger" onclick="kickUid(${Number(uid)})">踢下线</button>` : ""}
      <button class="btn small secondary" onclick="prefillGive(${Number(uid)})">给此人发物品</button>
    </div>`;
}

window.inspectPlayer = async (uid) => {
  switchTab("accounts");
  await showPlayer(uid);
};

window.showPlayer = showPlayer;

window.kickUid = async (uid) => {
  const r = await api(`/api/kick/${uid}`, { method: "POST" });
  toast(r.kicked ? `已踢出 UID ${uid}` : `UID ${uid} 当前不在线`);
  refreshActive();
};

window.deletePlayerData = async (uid) => {
  if (!confirm(`确认清空 UID ${uid} 的全部玩家存档数据？账号记录将保留。`)) return;
  const r = await api(`/api/accounts/${uid}`, { method: "DELETE" });
  toast(r.deleted ? `已清空 UID ${uid} 存档` : `未找到 UID ${uid}`);
  if (state.selectedUid === uid) {
    state.selectedUid = null;
    $("#player-detail-body").textContent = "点击左侧任意账号查看详情与货币数据。";
    $("#detail-status").classList.add("hidden");
  }
  loadAccounts();
};

window.prefillGive = (uid) => {
  $("#give-uid").value = uid;
  switchTab("grant");
};

async function loadCommands() {
  const cmds = await api("/api/commands");
  $("#cmd-list").innerHTML = cmds.map((c) => `
    <button type="button" class="cmd-item" onclick="fillCommand('${esc(c.name)}')">
      <div class="cmd-name">${esc(c.name)}</div>
      <div class="cmd-usage">${esc(c.usage)}</div>
      <div class="cmd-desc">${esc(c.description)}</div>
    </button>`).join("");
}

window.fillCommand = (name) => {
  const input = $("#console-cmd");
  input.value = `${name} `;
  input.focus();
};

async function loadSetup() {
  const s = await api("/api/setup");
  state.setupData = s;
  const ports = s.ports || {};
  $("#setup-ports").innerHTML = `
    <div class="kv-row"><span class="muted">SDK 公开地址</span><span class="mono">${esc(s.public_address)}</span></div>
    <div class="kv-row"><span class="muted">游戏服地址</span><span class="mono">${esc(s.game_public_address)}</span></div>
    <div class="kv-row"><span class="muted">游戏 TCP 端口</span><span class="mono">${esc(ports.game)}</span></div>
    <div class="kv-row"><span class="muted">SDK HTTPS 端口</span><span class="mono">${esc(ports.http)}</span></div>
    <div class="kv-row"><span class="muted">管理 HTTP 端口</span><span class="mono">${esc(ports.admin)}</span></div>
    <div class="kv-row"><span class="muted">本机局域网 IP</span><span class="mono">${esc((s.addresses || []).join(", ") || "-")}</span></div>`;

  const hostIp = s.public_address || "127.0.0.1";
  const lines = (s.cn_domains || []).map((d) => `${hostIp} ${d}`).join("\n");
  $("#hosts-preview").textContent = lines;

  const active = s.hosts || [];
  $("#hosts-state-note").textContent = active.length
    ? `当前 hosts 已写入 ${active.length} 条重定向规则。`
    : "当前 hosts 未写入重定向规则（或需以管理员权限操作）。";

  $("#global-domains").innerHTML = (s.global_domains || [])
    .map((d) => `<span class="tag">${esc(d)}</span>`)
    .join("");
}

async function refreshActive() {
  try {
    if (state.tab === "overview") await loadOverview();
    else if (state.tab === "accounts") await loadAccounts();
    else if (state.tab === "console") await loadCommands();
    else if (state.tab === "setup") await loadSetup();
    $("#btn-logout").classList.toggle("hidden", !state.token);
  } catch (err) {
    if (err.message !== "需要管理员 Token") {
      $("#brand-status").textContent = "连接异常";
      $("#brand-status").className = "brand-sub err";
    }
  }
}

$("#nav").addEventListener("click", (e) => {
  const btn = e.target.closest(".nav-btn");
  if (btn) switchTab(btn.dataset.tab);
});

$("#btn-refresh").addEventListener("click", () => refreshActive());
$("#btn-save-all").addEventListener("click", async () => {
  const r = await runGm("server save");
  toast(r.messages?.[0] || "已保存");
});

$("#form-search").addEventListener("submit", (e) => { e.preventDefault(); loadAccounts(); });
$("#form-create-account").addEventListener("submit", async (e) => {
  e.preventDefault();
  const input = $("#new-email");
  const raw = input.value.trim();
  const email = raw.includes("@") ? raw : `${raw}@local`;
  const r = await api("/api/accounts", { method: "POST", body: JSON.stringify({ email }) });
  toast(`账号就绪: UID ${r.uid} (${r.email})`);
  input.value = "";
  loadAccounts();
});

$("#form-give").addEventListener("submit", async (e) => {
  e.preventDefault();
  const g = $("#give-genre").value;
  const d = $("#give-detail").value;
  const p = $("#give-particular").value;
  const c = $("#give-count").value;
  const uid = $("#give-uid").value.trim();
  const q = uid ? `?uid=${encodeURIComponent(uid)}` : "";
  const r = await api(`/api/give/${g}/${d}/${p}/${c}${q}`);
  toast(`已发放模板 ${r.template} ×${r.count}（命中 ${r.given_to} 人）`);
});

$("#form-gm-money").addEventListener("submit", async (e) => {
  e.preventDefault();
  const r = await runGm(`grant money ${$("#gm-money-type").value} ${$("#gm-money-val").value}`);
  toast(r.messages?.[0] || "完成");
});
$("#form-gm-level").addEventListener("submit", async (e) => {
  e.preventDefault();
  const r = await runGm(`grant level ${$("#gm-level-val").value}`);
  toast(r.messages?.[0] || "完成");
});
$("#form-gm-mail").addEventListener("submit", async (e) => {
  e.preventDefault();
  const title = $("#gm-mail-title").value.trim();
  if (!title) return;
  const r = await runGm(`mail ${title}`);
  toast(r.messages?.[0] || "已发送");
  $("#gm-mail-title").value = "";
});
$$("[data-gm]").forEach((btn) => btn.addEventListener("click", async () => {
  const r = await runGm(btn.dataset.gm);
  toast(r.messages?.[0] || "已执行");
}));

$("#form-console").addEventListener("submit", async (e) => {
  e.preventDefault();
  const input = $("#console-cmd");
  const cmd = input.value.trim();
  if (!cmd) return;
  state.history.unshift(cmd);
  state.historyIdx = -1;
  input.value = "";
  try { await runGm(cmd, true); } catch (err) { appendLog(err.message, "log-err"); }
});

$("#console-cmd").addEventListener("keydown", (e) => {
  if (e.key === "ArrowUp" && state.history.length) {
    e.preventDefault();
    state.historyIdx = Math.min(state.history.length - 1, state.historyIdx + 1);
    e.target.value = state.history[state.historyIdx];
  } else if (e.key === "ArrowDown") {
    e.preventDefault();
    state.historyIdx = Math.max(-1, state.historyIdx - 1);
    e.target.value = state.historyIdx >= 0 ? state.history[state.historyIdx] : "";
  }
});

$("#btn-clear-log").addEventListener("click", () => { $("#console-log").innerHTML = ""; });

$("#btn-redirect-status").addEventListener("click", async () => {
  const r = await runGm("redirect status");
  toast(r.messages?.[0] || "");
  loadSetup();
});
$("#btn-redirect-on").addEventListener("click", async () => {
  const r = await runGm("redirect on");
  toast(r.messages?.[0] || "", !r.success || (r.messages?.[0] || "").includes("denied"));
  loadSetup();
});
$("#btn-redirect-off").addEventListener("click", async () => {
  const r = await runGm("redirect off");
  toast(r.messages?.[0] || "", !r.success || (r.messages?.[0] || "").includes("denied"));
  loadSetup();
});
$("#btn-copy-hosts").addEventListener("click", async () => {
  const text = $("#hosts-preview").textContent;
  try {
    await navigator.clipboard.writeText(text);
    toast("已复制到剪贴板");
  } catch {
    toast("浏览器禁止自动复制，请手动框选复制", true);
  }
});

$("#form-auth").addEventListener("submit", async (e) => {
  e.preventDefault();
  state.token = $("#auth-token-input").value.trim();
  localStorage.setItem("stc_admin_token", state.token);
  $("#auth-error").classList.add("hidden");
  try {
    await api("/api/overview");
    $("#auth-modal").classList.add("hidden");
    $("#auth-token-input").value = "";
    refreshActive();
  } catch {
    $("#auth-error").classList.remove("hidden");
  }
});

$("#btn-logout").addEventListener("click", () => {
  state.token = "";
  localStorage.removeItem("stc_admin_token");
  refreshActive();
});

setInterval(() => {
  if ($("#auto-refresh").checked && (state.tab === "overview" || state.tab === "accounts")) {
    refreshActive();
  }
}, 5000);

refreshActive();

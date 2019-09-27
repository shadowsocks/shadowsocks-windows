const vm = require("vm");
const fs = require("fs");
const pacRuntime = require("./nsProxyAutoConfig");

const abpContent = fs.readFileSync("../shadowsocks-csharp/Data/abp.js");
const exportFindProxyForURL = "fppu = FindProxyForURL";
let abpcontext = {
    __USERRULES__: [],
    __RULES__: [],
    fppu: undefined
};

function RunPACInVM(url, host) {
    // Run each PAC in new context
    const ctx = JSON.parse(JSON.stringify(abpcontext));
    Object.assign(ctx, pacRuntime);
    vm.createContext(ctx);
    vm.runInContext(abpContent, ctx);
    vm.runInContext(exportFindProxyForURL, ctx);
    if (host === undefined) {
        host = new URL(url).hostname;
    }
    return ctx.fppu(url, host);
}

module.exports = {
    FindProxyForURL: RunPACInVM,
    get userrules() {
        return abpcontext.__USERRULES__;
    },
    set userrules(r) {
        abpcontext.__USERRULES__ = r;
    },
    get rules() {
        return abpcontext.__RULES__;
    },
    set rules(r) {
        abpcontext.__RULES__ = r;
    }
};

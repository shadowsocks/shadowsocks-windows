const vm = require("vm")
const fs = require("fs")
const pacRuntime = require("./nsProxyAutoConfig")

const abpContent = fs.readFileSync("../shadowsocks-csharp/Data/abp.js");
const exportFindProxyForURL = "fppu = FindProxyForURL";
let abpcontext = {
    __USERRULES__: [],
    __RULES__: [],
    fppu: undefined
}
Object.assign(abpcontext, pacRuntime)
vm.createContext(abpcontext)
vm.runInContext(abpContent, abpcontext)
vm.runInContext(exportFindProxyForURL, abpcontext)
module.exports = {
    FindProxyForURL: abpcontext.fppu,
    get userrules() { return abpcontext.__USERRULES__ },
    set userrules(r) { abpcontext.__USERRULES__ = r },
    get rules() { return abpcontext.__RULES__ },
    set rules(r) { abpcontext.__RULES__ = r },
}

const abp = require("../abpwrapper");

describe("||api.ai", () => {
    it("Shouldnt match api.aixcoder.com", () => {
        abp.rules = ["||api.ai"];
        expect(abp.FindProxyForURL("https://api.aixcoder.com/getmodels")).toBe(
            abp.direct
        );
    });
    it("Should match api.aixcoder", () => {
        abp.rules = ["||api.ai"];
        expect(abp.FindProxyForURL("https://api.ai/")).toBe(abp.proxy);
    });
    it("||ip138.co shouldnt match ip138.com", () => {
        abp.rules = ["||ip138.co"];
        expect(abp.FindProxyForURL("http://www.ip138.com")).toBe(abp.direct);
    });
});

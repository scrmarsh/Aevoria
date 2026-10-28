import fs from 'node:fs';
import zlib from 'node:zlib';
function png(path) {
    const b=fs.readFileSync(path),w=b.readUInt32BE(16),h=b.readUInt32BE(20),c=b[25]===6?4:3,a=[];
    if(b[24]!==8||![2,6].includes(b[25]))throw Error('Expected 8 bit RGB/RGBA PNG');
    for(let p=8;p<b.length;){const n=b.readUInt32BE(p);if(b.toString('ascii',p+4,p+8)==='IDAT')a.push(b.subarray(p+8,p+8+n));p+=n+12;}
    const r=zlib.inflateSync(Buffer.concat(a)),s=w*c,o=Buffer.alloc(h*s);
    for(let y=0;y<h;y++)for(let x=0;x<s;x++) {
        const f=r[y*(s+1)],i=y*s+x,l=x>=c?o[i-c]:0,u=y?o[i-s]:0,ul=y&&x>=c?o[i-s-c]:0;
        let v=0;if(f===1)v=l;if(f===2)v=u;if(f===3)v=(l+u)>>1;
        if(f===4){const q=l+u-ul,A=Math.abs(q-l),B=Math.abs(q-u),C=Math.abs(q-ul);v=A<=B&&A<=C?l:B<=C?u:ul;}
        o[i]=(r[y*(s+1)+1+x]+v)&255;
    }
    return {w,h,c,o,pixel(x,y){const i=(y*w+x)*c;return [...o.subarray(i,i+3)];}};
}
const root=process.argv[2],names=['','-restored','-without-water','-opaque-diagnostic','-depth-diagnostic','-alpha-diagnostic'];
const images=Object.fromEntries(names.filter(n=>fs.existsSync(root+n+'.png')).map(n=>[n||'normal',png(root+n+'.png')]));
for(const [x,y] of [[900,800],[1000,700],[1200,700],[600,900],[800,825],[1400,740]])console.log(x,y,Object.fromEntries(Object.entries(images).map(([name,p])=>[name,p.pixel(x,y)])));

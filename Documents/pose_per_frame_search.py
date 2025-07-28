import os, json, math, argparse
from dataclasses import dataclass
import numpy as np
import cv2
import onnxruntime as ort
from tqdm import tqdm

# ===== MediaPipe Pose 33 接続（描画用） =====
POSE_CONNECTIONS = [
    (11,12),(11,23),(12,24),(23,24),
    (12,14),(14,16),(16,22),(16,20),(20,18),
    (11,13),(13,15),(15,21),(15,19),(19,17),
    (24,26),(26,28),(28,32),(32,30),
    (23,25),(25,27),(27,31),(31,29),
    (0,1),(1,2),(2,3),(3,7),(0,4),(4,5),(5,6),(6,8),
    (11,0),(12,0)
]
VIS_TH = 0.5

# 左右対応（MediaPipe 33）
LR_MAP = np.array([
    0, 4, 5, 6, 1, 2, 3, 8, 7, 10, 9,
    12,11, 14,13, 16,15, 18,17, 20,19, 22,21,
    24,23, 26,25, 28,27, 30,29, 32,31
], dtype=np.int32)

@dataclass
class ModelIOInfo:
    input_name: str
    input_h: int
    input_w: int
    is_nchw: bool

def get_model_io_info(session: ort.InferenceSession) -> ModelIOInfo:
    inp = session.get_inputs()[0]
    name = inp.name
    shape = [(int(s) if isinstance(s, (int, np.integer)) else s) for s in inp.shape]
    if len(shape)==4:
        if shape[-1]==3:  # NHWC
            h = int(shape[1] if isinstance(shape[1], (int,np.integer)) else 256)
            w = int(shape[2] if isinstance(shape[2], (int,np.integer)) else 256)
            return ModelIOInfo(name,h,w,is_nchw=False)
        if shape[1]==3:   # NCHW
            h = int(shape[2] if isinstance(shape[2], (int,np.integer)) else 256)
            w = int(shape[3] if isinstance(shape[3], (int,np.integer)) else 256)
            return ModelIOInfo(name,h,w,is_nchw=True)
    return ModelIOInfo(name,256,256,is_nchw=False)

def preprocess_patch(patch_bgr, io: ModelIOInfo):
    img = cv2.cvtColor(patch_bgr, cv2.COLOR_BGR2RGB).astype(np.float32)/255.0
    if io.is_nchw: x = np.transpose(img,(2,0,1))[None,...]
    else: x = img[None,...]
    return x

def parse_outputs(outputs):
    cand_lm, cand_world = None, None
    for out in outputs:
        arr = np.array(out)
        if arr.ndim>=2 and arr.shape[-2:]==(33,5): cand_lm=arr.reshape(33,5).astype(np.float32)
        if arr.ndim>=2 and arr.shape[-2:]==(33,3): cand_world=arr.reshape(33,3).astype(np.float32)
    if cand_lm is None or cand_world is None:
        for out in outputs:
            flat = np.array(out).reshape(-1)
            n=flat.size
            if cand_lm is None and n>=165: cand_lm=flat[:165].reshape(33,5).astype(np.float32)
            if cand_world is None:
                if n>=165+99: cand_world=flat[165:165+99].reshape(33,3).astype(np.float32)
                elif n==99:   cand_world=flat.reshape(33,3).astype(np.float32)
    return cand_lm, cand_world

def lm_to_patch_pixels(lm_33x5, patch_w, patch_h):
    if lm_33x5 is None: return None
    lm = lm_33x5.copy()
    xs, ys = lm[:,0], lm[:,1]
    # 0..1 正規化想定。>2 なら px とみなす
    if float(np.nanmax(xs))<=2.0 and float(np.nanmax(ys))<=2.0:
        xs = xs*patch_w; ys = ys*patch_h
    lm[:,0], lm[:,1] = xs, ys
    return lm

def affine_from_center(cx, cy, half_size, angle_deg, dst_w, dst_h):
    T1 = np.array([[1,0,-cx],[0,1,-cy],[0,0,1]], np.float32)
    ang=-angle_deg; rad=math.radians(ang); c,s=math.cos(rad), math.sin(rad)
    R = np.array([[c,-s,0],[s,c,0],[0,0,1]], np.float32)
    sx = dst_w/(half_size*2.0); sy = dst_h/(half_size*2.0)
    S = np.array([[sx,0,0],[0,sy,0],[0,0,1]], np.float32)
    T2 = np.array([[1,0,dst_w*0.5],[0,1,dst_h*0.5],[0,0,1]], np.float32)
    A = T2 @ S @ R @ T1
    return A[:2,:]

def invert_affine(A2x3): return cv2.invertAffineTransform(A2x3)

def apply_inv_affine(points_xy, invA):
    if points_xy is None: return None
    ones = np.ones((points_xy.shape[0],1), np.float32)
    P = np.hstack([points_xy.astype(np.float32), ones])  # (N,3)
    return (invA @ P.T).T

def draw_pose(img_bgr, lm_pix):
    if lm_pix is None: return img_bgr
    img=img_bgr.copy()
    pts=[]
    for i in range(lm_pix.shape[0]):
        x,y,z = lm_pix[i,0], lm_pix[i,1], lm_pix[i,2]
        vis = lm_pix[i,3] if lm_pix.shape[1]>=4 else 1.0
        if np.isnan(x) or np.isnan(y): pts.append(None); continue
        pts.append((int(round(x)), int(round(y)), float(vis)))
    for a,b in POSE_CONNECTIONS:
        if a<len(pts) and b<len(pts):
            pa,pb=pts[a],pts[b]
            if pa and pb and pa[2]>=VIS_TH and pb[2]>=VIS_TH:
                cv2.line(img,(pa[0],pa[1]),(pb[0],pb[1]),(0,255,0),2,cv2.LINE_AA)
    for p in pts:
        if p and p[2]>=VIS_TH:
            cv2.circle(img,(p[0],p[1]),3,(0,128,255),-1,lineType=cv2.LINE_AA)
    return img

def flip_remap_patch_landmarks(lm_patch, patch_w, world=None, world_flip_sign=True):
    if lm_patch is None: return None, None
    out = lm_patch.copy()
    out[:,0] = patch_w - out[:,0]
    out = out[LR_MAP]
    w_out=None
    if world is not None:
        w_out = world.copy()
        if world_flip_sign: w_out[:,0] = -w_out[:,0]
        w_out = w_out[LR_MAP]
    return out, w_out

def pose_score(lm_patch, patch_w, patch_h):
    if lm_patch is None: return -1e9
    # visibility があれば平均で評価
    if lm_patch.shape[1] >= 4:
        v = lm_patch[:,3]
        v = v[np.isfinite(v)]
        if len(v): return float(np.mean(v))
    # フォールバック：スケルトン辺の平均長（スケール正規化）
    pts = lm_patch[:,:2]
    lengths=[]
    for a,b in POSE_CONNECTIONS:
        pa,pb=pts[a],pts[b]
        if np.any(np.isnan(pa)) or np.any(np.isnan(pb)): continue
        lengths.append(np.linalg.norm(pa-pb))
    if not lengths: return -1e9
    return float(np.mean(lengths)/max(patch_w,patch_h))

def tta_infer_on_patch(session, io, patch_bgr, do_flip=True):
    # 通常
    x = preprocess_patch(patch_bgr, io)
    outs = session.run(None, {io.input_name: x})
    lm, world = parse_outputs(outs)
    lm = lm_to_patch_pixels(lm, io.input_w, io.input_h)

    # Flip TTA
    if do_flip:
        patch_flipped = cv2.flip(patch_bgr, 1)
        x2 = preprocess_patch(patch_flipped, io)
        outs2 = session.run(None, {io.input_name: x2})
        lm2, world2 = parse_outputs(outs2)
        lm2 = lm_to_patch_pixels(lm2, io.input_w, io.input_h)
        lm2r, world2r = flip_remap_patch_landmarks(lm2, io.input_w, world2, world_flip_sign=True)

        if lm is not None and lm2r is not None:
            if lm.shape[1]>=4 and lm2r.shape[1]>=4:
                w1 = np.clip(lm[:,3:4], 0.1, 1.0)
                w2 = np.clip(lm2r[:,3:4], 0.1, 1.0)
                lm = (lm*w1 + lm2r*w2) / (w1+w2)
            else:
                lm = 0.5*(lm + lm2r)
        if world is not None and world2r is not None:
            world = 0.5*(world + world2r)

    return lm, world

def search_best_per_frame(session, io, frame_bgr, angles, scales, center_grid=1, tta_flip=True):
    H, W = frame_bgr.shape[:2]
    # 中心候補（1 or 3）
    if center_grid == 3:
        xs = np.linspace(W*0.40, W*0.60, 3)
        ys = np.linspace(H*0.45, H*0.65, 3)
        centers = [(cx, cy) for cx in xs for cy in ys]
    else:
        centers = [(W*0.5, H*0.55)]

    best = {"score": -1e9, "lm_pix": None, "world": None}

    for (cx,cy) in centers:
        for s in scales:
            half = float(s)*min(W,H)  # s は [0..1] の比率（半径）
            for ang in angles:
                A = affine_from_center(cx, cy, half, ang, io.input_w, io.input_h)
                patch = cv2.warpAffine(frame_bgr, A, (io.input_w, io.input_h),
                                       flags=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
                lm_p, world = tta_infer_on_patch(session, io, patch, do_flip=tta_flip)
                sc = pose_score(lm_p, io.input_w, io.input_h)
                if sc > best["score"]:
                    invA = cv2.invertAffineTransform(A)
                    lm_pix = None
                    if lm_p is not None:
                        xy_img = apply_inv_affine(lm_p[:,:2], invA)
                        lm_pix = lm_p.copy()
                        lm_pix[:,0:2] = xy_img
                    best = {"score": sc, "lm_pix": lm_pix, "world": world}
    return best["lm_pix"], best["world"], best["score"]

def write_ndjson_line(f, frame_idx, t_sec, lm_pix, world):
    rec={"frame_index":frame_idx,"timestamp_sec":t_sec,"landmarks":None,"world_landmarks":None}
    if lm_pix is not None:
        L=[]
        for i in range(lm_pix.shape[0]):
            x,y,z = float(lm_pix[i,0]), float(lm_pix[i,1]), float(lm_pix[i,2])
            vis = float(lm_pix[i,3]) if lm_pix.shape[1]>=4 else 1.0
            pres = float(lm_pix[i,4]) if lm_pix.shape[1]>=5 else 1.0
            L.append({"x":x,"y":y,"z":z,"visibility":vis,"presence":pres})
        rec["landmarks"]=L
    if world is not None:
        rec["world_landmarks"]=[{"x":float(p[0]),"y":float(p[1]),"z":float(p[2])} for p in world.tolist()]
    f.write(json.dumps(rec, ensure_ascii=False)+"\n")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--video", default="DanceMovie.mp4")
    ap.add_argument("--model", default="pose_landmark_full.onnx")
    ap.add_argument("--out_video", default="DanceMovie_pose_overlay.mp4")
    ap.add_argument("--out_jsonl", default="DanceMovie_pose_landmarks.ndjson")
    ap.add_argument("--device", default="cpu", choices=["cpu","cuda"])
    # スキャン設定（デフォルトは軽め）
    ap.add_argument("--angles", type=str, default="-40,-20,0,20,40")
    ap.add_argument("--scales", type=str, default="0.32,0.38,0.44")  # half_size 比率（min(W,H)に対する）
    ap.add_argument("--center_grid", type=int, default=1, choices=[1,3])
    ap.add_argument("--tta_flip", type=int, default=1)
    ap.add_argument("--dump_frames", action="store_true")
    args = ap.parse_args()

    angles = [float(a) for a in args.angles.split(",") if a.strip()]
    scales = [float(s) for s in args.scales.split(",") if s.strip()]
    tta_flip = bool(args.tta_flip)

    providers = ['CPUExecutionProvider'] if args.device=="cpu" else ['CUDAExecutionProvider','CPUExecutionProvider']
    sess = ort.InferenceSession(args.model, providers=providers)
    io = get_model_io_info(sess)

    cap = cv2.VideoCapture(args.video)
    if not cap.isOpened(): raise RuntimeError(f"open video failed: {args.video}")
    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    W  = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    H  = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    vw = cv2.VideoWriter(args.out_video, cv2.VideoWriter_fourcc(*'mp4v'), fps, (W,H))
    if not vw.isOpened(): raise RuntimeError(f"open writer failed: {args.out_video}")

    if args.dump_frames and not os.path.exists("frames"):
        os.makedirs("frames", exist_ok=True)

    with open(args.out_jsonl,"w",encoding="utf-8") as fjson:
        total = int(cap.get(cv2.CAP_PROP_FRAME_COUNT)) or None
        pbar = tqdm(total=total, desc="Per-frame search")
        idx=0
        while True:
            ok, frame = cap.read()
            if not ok: break

            if args.dump_frames:
                cv2.imwrite(os.path.join("frames", f"frame_{idx:06d}.png"), frame)

            lm_pix, world, score = search_best_per_frame(
                sess, io, frame, angles=angles, scales=scales,
                center_grid=args.center_grid, tta_flip=tta_flip
            )
            over = draw_pose(frame, lm_pix)
            vw.write(over)
            write_ndjson_line(fjson, idx, idx/(fps if fps>0 else 30.0), lm_pix, world)

            idx += 1
            pbar.update(1)

        pbar.close()

    vw.release(); cap.release()
    print(f"[Done] overlay video  : {args.out_video}")
    print(f"[Done] landmarks JSONL: {args.out_jsonl}")

if __name__ == "__main__":
    main()
